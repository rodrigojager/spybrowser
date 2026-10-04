using Microsoft.Playwright;
using SpyBrowser.Core;
using SpyBrowser.Playwright;

namespace SpyBrowser.Tests;

[Collection(TimedInputCollection.Name)]
public sealed class FactoryStressAcceptanceTests
{
    private static readonly TimeSpan BrowserTimeout = TimeSpan.FromSeconds(60);

    [BrowserFact]
    public async Task Concurrent_disposable_contexts_do_not_lease_the_persistent_identity_profile()
    {
        using var ownedTemporaryDirectory = new TemporaryDirectory();
        var identity = BrowserIdentity.Create("factory-stress-no-profile-lease");
        var store = new IdentityStore(ownedTemporaryDirectory.Path);
        var persistentProfile = store.GetProfileDirectory(identity.Id);
        var leasePath = store.GetLeasePath(identity.Id);
        var options = new SpyBrowserLaunchOptions
        {
            IdentityId = identity.Id,
            IdentityOverride = identity,
            IdentitiesRoot = ownedTemporaryDirectory.Path,
            Headless = BrowserTestSettings.Headless,
            ChannelOverride = BrowserTestSettings.Channel,
            RunGpuProbe = false,
            GpuPolicyOverride = GpuPolicy.AllowSoftware,
            FailOnConsistencyErrors = false
        };

        await using var first = await SpyBrowserLauncher.LaunchContextAsync(options).WaitAsync(BrowserTimeout);
        await using var second = await SpyBrowserLauncher.LaunchContextAsync(options).WaitAsync(BrowserTimeout);
        var firstPage = await first.NewPageAsync();
        var secondPage = await second.NewPageAsync();
        await Task.WhenAll(
            firstPage.SetContentAsync("<p>first disposable context</p>"),
            secondPage.SetContentAsync("<p>second disposable context</p>"));

        Assert.Same(identity, first.Identity);
        Assert.Same(identity, second.Identity);
        Assert.NotSame(first.Browser, second.Browser);
        Assert.False(Directory.Exists(persistentProfile));
        Assert.False(File.Exists(leasePath));

        // A real persistent launch of this same identity/profile is the lease proof:
        // it must acquire the identity lease even while both in-memory contexts remain open.
        await using var persistent = await SpyBrowserLauncher.LaunchPersistentContextAsync(options).WaitAsync(BrowserTimeout);
        Assert.True(Directory.Exists(persistentProfile));
        Assert.True(File.Exists(leasePath));
        Assert.False(firstPage.IsClosed);
        Assert.False(secondPage.IsClosed);
        var persistentPage = await persistent.NewPageAsync();
        await persistentPage.SetContentAsync("<p>persistent profile remains independently usable</p>");
        Assert.Equal("persistent profile remains independently usable", await persistentPage.Locator("p").InnerTextAsync());
        Assert.False(firstPage.IsClosed);
        Assert.False(secondPage.IsClosed);
    }

    [BrowserFact]
    public async Task Concurrent_humanized_popups_keep_identity_and_other_pages_alive_when_one_closes_immediately()
    {
        using var ownedTemporaryDirectory = new TemporaryDirectory();
        var identity = BrowserIdentity.Create("factory-stress-popups");
        var interaction = new HumanInteractionOptions
        {
            MouseAlgorithm = MouseTrajectoryAlgorithm.Cursory,
            CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible,
            RandomSeed = 0x51A7,
            CursoryFrequency = 60,
            CursoryFrequencyRandomizer = 0,
            MouseMinimumDurationMilliseconds = 10,
            MouseMaximumDurationMilliseconds = 40,
            CursoryMovementDeadlineMilliseconds = 10_000,
            ActionDeadlineMilliseconds = 20_000,
            TypingDeadlineMilliseconds = 20_000,
            KeyMinimumDelayMilliseconds = 0,
            KeyMaximumDelayMilliseconds = 0,
            ClickHoldMinimumMilliseconds = 0,
            ClickHoldMaximumMilliseconds = 0
        };
        await using var handle = await SpyBrowserLauncher.LaunchBrowserAsync(new SpyBrowserLaunchOptions
        {
            IdentityId = identity.Id,
            IdentityOverride = identity,
            IdentitiesRoot = ownedTemporaryDirectory.Path,
            Headless = BrowserTestSettings.Headless,
            ChannelOverride = BrowserTestSettings.Channel,
            RunGpuProbe = false,
            GpuPolicyOverride = GpuPolicy.AllowSoftware,
            FailOnConsistencyErrors = false,
            Humanize = true,
            HumanInteraction = interaction,
            ConfigureBrowserLaunch = options =>
                options.Args = (options.Args ?? new List<string>()).Concat(new[] { "--disable-popup-blocking" }).ToList()
        });
        var context = await handle.NewContextAsync();
        var parent = await context.NewPageAsync();
        await parent.SetContentAsync("""
            <input id="parent" value="parent-stays-put">
            <button id="spawn" onclick="for(let i=0;i<3;i++){const p=window.open('about:blank','child'+i);p.document.write(`<input id=entry><button id=act>act</button><output id=result></output>`);p.document.close();}">spawn</button>
            """);

        var pageEvents = new System.Collections.Concurrent.ConcurrentBag<IPage>();
        var popupEvents = new System.Collections.Concurrent.ConcurrentBag<IPage>();
        var contextCallbackLocaleChecks = new System.Collections.Concurrent.ConcurrentBag<Task<string>>();
        var popupCallbackLocaleChecks = new System.Collections.Concurrent.ConcurrentBag<Task<string>>();
        var popupReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pageReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<IPage> pageHandler = (_, popup) =>
        {
            pageEvents.Add(popup);
            contextCallbackLocaleChecks.Add(popup.EvaluateAsync<string>("navigator.language"));
            if (pageEvents.Count >= 3) pageReady.TrySetResult();
        };
        EventHandler<IPage> popupHandler = (_, popup) =>
        {
            popupEvents.Add(popup);
            popupCallbackLocaleChecks.Add(popup.EvaluateAsync<string>("navigator.language"));
            if (popupEvents.Count >= 3) popupReady.TrySetResult();
        };
        context.Page += pageHandler;
        parent.Popup += popupHandler;
        await parent.Locator("#spawn").ClickAsync().WaitAsync(BrowserTimeout);
        await Task.WhenAll(pageReady.Task, popupReady.Task).WaitAsync(BrowserTimeout);

        var pages = pageEvents.ToArray();
        var popups = popupEvents.ToArray();
        Assert.Equal(3, pages.Length);
        Assert.Equal(3, popups.Length);
        Assert.All(await Task.WhenAll(contextCallbackLocaleChecks).WaitAsync(BrowserTimeout), locale => Assert.Equal(identity.Locale, locale));
        Assert.All(await Task.WhenAll(popupCallbackLocaleChecks).WaitAsync(BrowserTimeout), locale => Assert.Equal(identity.Locale, locale));
        Assert.Equal(3, context.Pages.Count(page => !ReferenceEquals(page, parent)));
        foreach (var fromContextEvent in pages)
        {
            var raw = PlaywrightHumanizer.Unwrap(fromContextEvent);
            var fromPopupEvent = Assert.Single(popups, candidate => ReferenceEquals(PlaywrightHumanizer.Unwrap(candidate), raw));
            var cachedWrapper = context.Pages.Single(candidate => ReferenceEquals(PlaywrightHumanizer.Unwrap(candidate), raw));
            Assert.Same(fromContextEvent, fromPopupEvent);
            Assert.Same(fromContextEvent, cachedWrapper);
            Assert.Same(context, fromContextEvent.Context);
        }

        var rawPopups = pages.Select(PlaywrightHumanizer.Unwrap).ToArray();
        foreach (var raw in rawPopups)
        {
            await raw.EvaluateAsync("""
                () => {
                    window.stress = { values: [], mouse: 0, positions: [] };
                    document.querySelector('#entry').addEventListener('input', e => window.stress.values.push(e.target.value));
                    document.querySelector('#act').addEventListener('click', () => document.querySelector('#result').textContent = 'acted');
                    document.addEventListener('mousemove', e => { window.stress.mouse++; window.stress.positions.push([e.clientX, e.clientY]); });
                }
                """);
        }

        var closing = pages[0];
        await closing.CloseAsync().WaitAsync(BrowserTimeout); // close one child immediately after publication and raw observation setup
        Assert.True(closing.IsClosed);
        Assert.DoesNotContain(context.Pages, candidate => ReferenceEquals(candidate, closing));

        // The surviving pages must start from their own unknown cursor position, despite the
        // opener having already been clicked: first move is one native anchor, next is generated.
        foreach (var popup in pages.Skip(1))
        {
            var raw = PlaywrightHumanizer.Unwrap(popup);
            await popup.Mouse.MoveAsync(25, 30).WaitAsync(BrowserTimeout);
            var anchorPositions = await raw.EvaluateAsync<double[][]>("window.stress.positions");
            Assert.InRange(anchorPositions.Length, 1, 2); // active-tab pointer handoff may report the opener position too
            Assert.Equal(new[] { 25d, 30d }, anchorPositions[^1]);
            await popup.Mouse.MoveAsync(150, 120).WaitAsync(BrowserTimeout);
            Assert.True(await raw.EvaluateAsync<int>("window.stress.mouse") > anchorPositions.Length + 1);
        }

        // The surviving popup wrappers act concurrently; raw-only listeners observe DOM effects.
        var texts = new[] { "alpha-popup", "bravo-popup", "charlie-popup" };
        var survivors = pages.Skip(1).Zip(texts.Skip(1)).ToArray();
        var childActions = survivors.Select(async pair =>
        {
            var (popup, text) = pair;
            await popup.Locator("#entry").PressSequentiallyAsync(text).WaitAsync(BrowserTimeout);
            await popup.Locator("#act").ClickAsync().WaitAsync(BrowserTimeout);
            Assert.Equal("acted", await popup.Locator("#result").InnerTextAsync().WaitAsync(BrowserTimeout));
            Assert.Equal(text, await popup.Locator("#entry").InputValueAsync().WaitAsync(BrowserTimeout));
        }).ToArray();
        await Task.WhenAll(childActions).WaitAsync(BrowserTimeout);
        Assert.Equal("parent-stays-put", await parent.Locator("#parent").InputValueAsync());
        foreach (var (popup, text) in survivors)
        {
            var raw = PlaywrightHumanizer.Unwrap(popup);
            Assert.Equal("acted", await raw.Locator("#result").InnerTextAsync());
            Assert.True(await raw.EvaluateAsync<int>("window.stress.mouse") > 0);
            Assert.Equal(text, await raw.EvaluateAsync<string>("document.querySelector('#entry').value"));
        }

        await closing.CloseAsync().WaitAsync(BrowserTimeout); // second close proves idempotent disposal
        Assert.True(closing.IsClosed);
        Assert.Equal(2, context.Pages.Count(page => !ReferenceEquals(page, parent)));
        foreach (var survivor in pages.Skip(1))
        {
            Assert.False(survivor.IsClosed);
            await survivor.Locator("#act").ClickAsync().WaitAsync(BrowserTimeout);
            Assert.Equal("acted", await survivor.Locator("#result").InnerTextAsync());
        }
        Assert.Equal("parent-stays-put", await parent.Locator("#parent").InputValueAsync());
        context.Page -= pageHandler;
        parent.Popup -= popupHandler;
        await context.CloseAsync().WaitAsync(BrowserTimeout);
    }
}
