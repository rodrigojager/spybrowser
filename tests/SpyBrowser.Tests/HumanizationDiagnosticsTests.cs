using System.Reflection;
using System.Text.Json;
using Microsoft.Playwright;
using SpyBrowser.Playwright;

namespace SpyBrowser.Tests;

public sealed class HumanizationDiagnosticsTests
{
    [Fact]
    public void Diagnostics_are_opt_in_and_capacity_is_validated()
    {
        var disabled = new PlaywrightHumanizer();
        Assert.Null(disabled.GetDiagnosticsSnapshot());
        Assert.Null(disabled.Scope.DiagnosticsForTesting);
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlaywrightHumanizer(new HumanInteractionOptions
        {
            EnableDiagnostics = true,
            DiagnosticsCapacity = 0
        }));
    }

    [Fact]
    public async Task Snapshot_is_bounded_and_does_not_include_arguments_or_exception_messages()
    {
        const string secret = "sentinel-private-selector-user-text-url-proxy-cookie-header";
        var originalError = new InvalidOperationException(secret);
        var raw = DispatchProxy.Create<IPage, FakePage>();
        ((FakePage)(object)raw).Error = originalError;
        var humanizer = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            EnableDiagnostics = true,
            DiagnosticsCapacity = 37
        });
        var page = humanizer.Wrap(raw);

        await page.GotoAsync("https://example.invalid/private?token=" + secret);
        try { await page.TitleAsync(); }
        catch (InvalidOperationException) { }

        for (var i = 0; i < 2_000; i++) await page.WaitForTimeoutAsync(0);
        try { await page.TitleAsync(); }
        catch (InvalidOperationException) { }
        var snapshot = humanizer.GetDiagnosticsSnapshot()!;
        var json = JsonSerializer.Serialize(snapshot);

        Assert.Equal(37, snapshot.Records.Count);
        Assert.Equal(1_966, snapshot.DroppedRecords);
        Assert.DoesNotContain(secret, json, StringComparison.Ordinal);
        Assert.Contains(snapshot.Records, record => record.Outcome == "error");
        Assert.Contains(snapshot.Records, record => record.Reason == "humanization.unsupported-surface");
        Assert.DoesNotContain(snapshot.Records, record => record.Method.Contains(secret, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Recorder_failures_never_fail_or_repeat_the_consumer_action()
    {
        var raw = DispatchProxy.Create<IPage, FakePage>();
        ((FakePage)(object)raw).Title = "consumer-result";
        var humanizer = new PlaywrightHumanizer(new HumanInteractionOptions { EnableDiagnostics = true });
        var wrapped = humanizer.Wrap(raw);
        var calls = 0;
        var failAt = 1;
        humanizer.DiagnosticFailureHookForTesting = () =>
        {
            if (Interlocked.Increment(ref calls) == failAt) throw new InvalidOperationException("recorder failure");
        };

        Assert.Equal("consumer-result", await wrapped.TitleAsync()); // Begin recorder failure.
        calls = 0;
        failAt = 2;
        Assert.Equal("consumer-result", await wrapped.TitleAsync()); // Completion recorder failure.
        Assert.Equal(2, ((FakePage)(object)raw).TitleCalls);
    }

    [Fact]
    public async Task Generic_task_result_and_exception_instance_are_preserved()
    {
        var originalError = new InvalidOperationException("must not leak");
        var raw = DispatchProxy.Create<IPage, FakePage>();
        var implementation = (FakePage)(object)raw;
        implementation.Error = originalError;
        implementation.FailOnTitleCall = 2;
        implementation.Title = "ok";
        var humanizer = new PlaywrightHumanizer(new HumanInteractionOptions { EnableDiagnostics = true });
        var wrapped = humanizer.Wrap(raw);

        Assert.Equal("ok", await wrapped.TitleAsync());
        var caught = await Assert.ThrowsAsync<InvalidOperationException>(() => wrapped.TitleAsync());
        Assert.Same(originalError, caught);
        Assert.Equal(new[] { "success", "error" }, humanizer.GetDiagnosticsSnapshot()!.Records.Select(record => record.Outcome));
    }

    [BrowserFact]
    public async Task Public_snapshot_reports_native_fill_and_humanized_mouse_without_payloads()
    {
        const string secret = "password-url-selector-cookie-header-proxy-secret";
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = BrowserTestSettings.Headless,
            Channel = BrowserTestSettings.Channel
        });
        var rawPage = await browser.NewPageAsync();
        await rawPage.SetContentAsync("<input id='private-selector'><script>window.moves=0;addEventListener('mousemove',()=>moves++);</script>");
        var humanizer = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            EnableDiagnostics = true,
            MouseAlgorithm = MouseTrajectoryAlgorithm.Cursory,
            CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible,
            CursoryMovementDeadlineMilliseconds = 10_000
        });
        var page = humanizer.Wrap<IPage>(rawPage);
        await page.Mouse.MoveAsync(10, 10);
        await page.Mouse.MoveAsync(400, 300);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(410, 310);
        await page.Mouse.UpAsync();
        await page.Locator("#private-selector").FillAsync(secret);
        await page.Locator("#private-selector").FillAsync(secret, new LocatorFillOptions { Timeout = 3_000 });

        var snapshot = humanizer.GetDiagnosticsSnapshot()!;
        var json = JsonSerializer.Serialize(snapshot);
        Assert.Contains(snapshot.Records, item => item.Mode == "humanized");
        Assert.Contains(snapshot.Records, item => item.Mode == "native-anchor" && item.Reason == "humanization.pointer-unknown");
        Assert.Contains(snapshot.Records, item => item.Mode == "native-anchor" && item.Reason == "humanization.active-drag");
        Assert.Contains(snapshot.Records, item => item.Method == "MoveAsync" && item.PlannedPoints is > 0 && item.DispatchedPoints is > 0 && item.FinalEndpointReached == true);
        Assert.Contains(snapshot.Records, item => item.Mode == "native-preserved-compatible-fill");
        Assert.Contains(snapshot.Records, item => item.Reason == "humanization.unsupported-options");
        Assert.Equal(browser.Version, snapshot.Provenance.BrowserVersion);
        Assert.Equal("cursory", snapshot.Provenance.Algorithm);
        Assert.StartsWith("cursory-js-", snapshot.Provenance.DatasetVersion, StringComparison.Ordinal);
        Assert.Equal("playwright.browser.version", snapshot.Provenance.BrowserVersionSource);
        Assert.DoesNotContain(secret, json, StringComparison.Ordinal);
        Assert.DoesNotContain("private-selector", json, StringComparison.Ordinal);
        Assert.True(await rawPage.EvaluateAsync<int>("moves") > 1);
    }

    [BrowserFact]
    public async Task Launch_handle_exposes_actual_browser_version_and_selected_channel_not_user_agent()
    {
        var identity = SpyBrowser.Core.BrowserIdentity.Create("runtime-provenance") with
        {
            Browser = new SpyBrowser.Core.BrowserIdentitySettings
            {
                Engine = SpyBrowser.Core.BrowserEngine.Chromium,
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/999.0.0.0"
            }
        };
        await using var handle = await SpyBrowserLauncher.LaunchContextAsync(new SpyBrowserLaunchOptions
        {
            IdentityId = identity.Id,
            IdentityOverride = identity,
            Headless = true,
            Humanize = true,
            RunGpuProbe = false
        });

        Assert.Equal("chromium", handle.RuntimeProvenance.BrowserFamily);
        Assert.Equal("playwright.launch-configuration", handle.RuntimeProvenance.BrowserFamilySource);
        Assert.Null(handle.RuntimeProvenance.BrowserChannel);
        Assert.Equal(handle.RawBrowser!.Version, handle.RuntimeProvenance.BrowserVersion);
        Assert.Equal("playwright.browser.version", handle.RuntimeProvenance.BrowserVersionSource);
        Assert.Equal("bezier:legacy-v1", handle.RuntimeProvenance.Algorithm);
        Assert.Equal("none", handle.RuntimeProvenance.Dataset);
        Assert.DoesNotContain("999.0.0.0", handle.RuntimeProvenance.BrowserVersion);
    }

    [Fact]
    public async Task Timeout_and_cancellation_have_stable_outcomes_without_messages()
    {
        const string secret = "sentinel-timeout-message";
        var timeoutPage = DispatchProxy.Create<IPage, FakePage>();
        ((FakePage)(object)timeoutPage).Error = new TimeoutException(secret);
        var canceledPage = DispatchProxy.Create<IPage, FakePage>();
        ((FakePage)(object)canceledPage).Canceled = true;
        var humanizer = new PlaywrightHumanizer(new HumanInteractionOptions { EnableDiagnostics = true });
        var timeoutWrapper = humanizer.Wrap(timeoutPage);
        var canceledWrapper = humanizer.Wrap(canceledPage);
        await Assert.ThrowsAsync<TimeoutException>(() => timeoutWrapper.TitleAsync());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledWrapper.TitleAsync());

        var snapshot = humanizer.GetDiagnosticsSnapshot()!;
        Assert.Contains(snapshot.Records, item => item.Outcome == "timeout");
        Assert.Contains(snapshot.Records, item => item.Outcome == "canceled");
        Assert.DoesNotContain(secret, JsonSerializer.Serialize(snapshot), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Concurrent_records_remain_bounded_and_count_overwrites()
    {
        var raw = DispatchProxy.Create<IPage, FakePage>();
        var humanizer = new PlaywrightHumanizer(new HumanInteractionOptions { EnableDiagnostics = true, DiagnosticsCapacity = 20 });
        var page = humanizer.Wrap(raw);
        await Task.WhenAll(Enumerable.Range(0, 200).Select(_ => page.WaitForTimeoutAsync(0)));

        var snapshot = humanizer.GetDiagnosticsSnapshot()!;
        Assert.Equal(20, snapshot.Records.Count);
        Assert.Equal(180, snapshot.DroppedRecords);
        Assert.Equal(20, snapshot.Records.Select(record => record.ActionId).Distinct().Count());
    }

    [Fact]
    public async Task Correlations_are_distinct_per_page_and_concurrent_invocation()
    {
        var humanizer = new PlaywrightHumanizer(new HumanInteractionOptions { EnableDiagnostics = true });
        var rawA = DispatchProxy.Create<IPage, FakePage>();
        var rawB = DispatchProxy.Create<IPage, FakePage>();
        var pageA = humanizer.Wrap(rawA);
        var pageB = humanizer.Wrap(rawB);
        await Task.WhenAll(pageA.WaitForTimeoutAsync(0), pageB.WaitForTimeoutAsync(0));
        var records = humanizer.GetDiagnosticsSnapshot()!.Records;
        Assert.Equal(2, records.Select(item => item.PageId).Distinct().Count());
        Assert.Equal(2, records.Select(item => item.ActionId).Distinct().Count());
    }

    public class FakePage : DispatchProxy
    {
        internal Exception? Error;
        internal string? Title;
        internal int FailOnTitleCall = 1;
        internal bool Canceled;
        private int _titleCalls;
        internal int TitleCalls => Volatile.Read(ref _titleCalls);
        private readonly IBrowserContext _context = DispatchProxy.Create<IBrowserContext, FakeContext>();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_Context") return _context;
            if (targetMethod?.Name == nameof(IPage.TitleAsync))
            {
                var call = Interlocked.Increment(ref _titleCalls);
                if (Canceled) return Task.FromCanceled<string>(new CancellationToken(canceled: true));
                if (Error is not null && call >= FailOnTitleCall) return Task.FromException<string>(Error);
                return Task.FromResult(Title ?? "");
            }
            if (targetMethod?.ReturnType == typeof(Task)) return Task.CompletedTask;
            if (targetMethod?.ReturnType.IsGenericType == true && targetMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                return typeof(FakePage).GetMethod(nameof(Completed), BindingFlags.Static | BindingFlags.NonPublic)!
                    .MakeGenericMethod(targetMethod.ReturnType.GetGenericArguments()[0]).Invoke(null, null);
            }
            var type = targetMethod?.ReturnType;
            return type is null || type == typeof(void) ? null : type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        private static Task<T> Completed<T>() => Task.FromResult(default(T)!);
    }

    public class FakeContext : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_Pages" ? Array.Empty<IPage>() : null;
    }
}
