using Microsoft.Playwright;
using SpyBrowser.Playwright;
using SpyBrowser.Playwright.Humanization;

namespace SpyBrowser.Tests;

/// <summary>Loopback-browser comparisons for the PlaywrightCompatible action contract.</summary>
public sealed class ActionContractMatrixTests
{
    private static HumanizationCompatibilityMode Compatible => HumanizationCompatibilityMode.PlaywrightCompatible;

    [BrowserFact]
    public async Task Fill_and_clear_match_raw_for_native_controls_and_javascript_controlled_input()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync("""
            <input id="raw-text"><input id="wrap-text">
            <input id="raw-password" type="password"><input id="wrap-password" type="password">
            <input id="raw-number" type="number"><input id="wrap-number" type="number">
            <input id="raw-date" type="date"><input id="wrap-date" type="date">
            <textarea id="raw-area"></textarea><textarea id="wrap-area"></textarea>
            <div id="raw-edit" contenteditable="true"></div><div id="wrap-edit" contenteditable="true"></div>
            <input id="raw-controlled"><input id="wrap-controlled">
            <script>
              window.trace={};
              for (const el of document.querySelectorAll('input,textarea,[contenteditable]')) {
                const id=el.id; trace[id]=[];
                for (const name of ['keydown','keyup','input','change']) el.addEventListener(name,e=>trace[id].push(name));
                if (id.endsWith('controlled')) el.addEventListener('input',()=>el.value=el.value.toUpperCase());
              }
            </script>
            """);
        var wrapped = new PlaywrightHumanizer(new HumanInteractionOptions { CompatibilityMode = Compatible }).Wrap(page);
        var controls = new (string Id, string Value)[]
        {
            ("text", "ordinary"), ("password", "sentinel-not-a-secret"), ("number", "42.5"),
            ("date", "2024-03-14"), ("area", "two\nlines"), ("edit", "editable text"), ("controlled", "mixed")
        };

        foreach (var (id, value) in controls)
        {
            var raw = page.Locator($"#raw-{id}");
            var proxy = wrapped.Locator($"#wrap-{id}");
            await page.EvaluateAsync($"trace['raw-{id}']=[];trace['wrap-{id}']=[]");
            await raw.FillAsync(value);
            var rawFillTrace = await page.EvaluateAsync<string[]>($"trace['raw-{id}']");
            await page.EvaluateAsync($"trace['raw-{id}']=[];trace['wrap-{id}']=[]");
            await proxy.FillAsync(value);
            var wrappedFillTrace = await page.EvaluateAsync<string[]>($"trace['wrap-{id}']");
            Assert.Equal(await ReadValue(raw, id), await ReadValue(proxy, id));
            Assert.Contains("input", rawFillTrace);
            Assert.Contains("input", wrappedFillTrace);
            Assert.DoesNotContain("keydown", wrappedFillTrace);
            Assert.DoesNotContain("keyup", wrappedFillTrace);

            await page.EvaluateAsync($"trace['raw-{id}']=[];trace['wrap-{id}']=[]");
            await raw.ClearAsync();
            var rawClearTrace = await page.EvaluateAsync<string[]>($"trace['raw-{id}']");
            await page.EvaluateAsync($"trace['raw-{id}']=[];trace['wrap-{id}']=[]");
            await proxy.ClearAsync();
            var wrappedClearTrace = await page.EvaluateAsync<string[]>($"trace['wrap-{id}']");
            Assert.Equal(await ReadValue(raw, id), await ReadValue(proxy, id));
            Assert.True(rawClearTrace.Count(name => name is "keydown" or "keyup") == wrappedClearTrace.Count(name => name is "keydown" or "keyup"), $"Native clear keyboard-event count differed for {id}.");
        }
    }

    [BrowserFact]
    public async Task Fill_routes_options_actionability_and_frame_routes_match_native_contract()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync("<input id='raw'><input id='wrapped'><input id='hidden' style='display:none'><input id='disabled' disabled><input class='many'><input class='many'><iframe id='child' srcdoc='<input id=&quot;field&quot;>'></iframe>");
        var wrapped = new PlaywrightHumanizer(new HumanInteractionOptions { CompatibilityMode = Compatible }).Wrap(page);

        await page.FillAsync("#raw", "zero", new PageFillOptions { Timeout = 0 });
        var explicitOptions = new LocatorFillOptions { Timeout = 0 };
        await wrapped.Locator("#wrapped").FillAsync("zero", explicitOptions);
        Assert.Equal(0, explicitOptions.Timeout);
        Assert.Equal(await page.Locator("#raw").InputValueAsync(), await page.Locator("#wrapped").InputValueAsync());
        await page.Locator("#raw").ClearAsync(new LocatorClearOptions());
        await wrapped.Locator("#wrapped").ClearAsync(new LocatorClearOptions());
        Assert.Equal(string.Empty, await page.Locator("#raw").InputValueAsync());
        Assert.Equal(string.Empty, await page.Locator("#wrapped").InputValueAsync());

        foreach (var selector in new[] { "#hidden", "#disabled", ".many" })
        {
            var rawError = await Record.ExceptionAsync(() => page.Locator(selector).FillAsync("x", new LocatorFillOptions { Timeout = 100 }));
            var wrappedError = await Record.ExceptionAsync(() => wrapped.Locator(selector).FillAsync("x", new LocatorFillOptions { Timeout = 100 }));
            Assert.NotNull(rawError);
            Assert.NotNull(wrappedError);
            Assert.False(page.IsClosed, $"Raw/wrapped actionability probing closed the page for {selector}.");
            if (selector == ".many")
            {
                Assert.Contains("strict", rawError!.Message, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("strict", wrappedError!.Message, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                Assert.True(IsTimeoutCategory(rawError!), $"Unexpected raw actionability exception for {selector}: {rawError.GetType().Name}: {rawError.Message}");
                Assert.True(IsTimeoutCategory(wrappedError!), $"Unexpected wrapped actionability exception for {selector}: {wrappedError.GetType().Name}: {wrappedError.Message}");
            }
        }

        await page.FillAsync("#raw", "page-route");
        await wrapped.FillAsync("#wrapped", "page-route");
        Assert.Equal(await page.Locator("#raw").InputValueAsync(), await page.Locator("#wrapped").InputValueAsync());
        var rawFrame = page.FrameLocator("#child");
        var wrappedFrame = wrapped.FrameLocator("#child");
        await rawFrame.Locator("#field").FillAsync("frame-route");
        await wrappedFrame.Locator("#field").FillAsync("frame-route");
        Assert.Equal("frame-route", await rawFrame.Locator("#field").InputValueAsync());
        Assert.Equal(await rawFrame.Locator("#field").InputValueAsync(), await wrappedFrame.Locator("#field").InputValueAsync());
    }

    [BrowserFact]
    public async Task Insert_press_and_type_keep_distinct_event_traces_and_unicode_order()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync("<input id='field'><script>window.trace=[];for(const n of ['keydown','keyup','input','change'])field.addEventListener(n,e=>trace.push(n+':'+(e.key||'')))</script>");
        var wrapped = new PlaywrightHumanizer(new HumanInteractionOptions { CompatibilityMode = Compatible }).Wrap(page);
        var field = wrapped.Locator("#field");
        await field.FocusAsync();
        await wrapped.Keyboard.InsertTextAsync("insert");
        var insertTrace = await page.EvaluateAsync<string[]>("trace");
        Assert.Equal(new[] { "input:" }, insertTrace);
        await field.PressAsync("Control+A");
        await field.PressAsync("Backspace");
        await field.PressAsync("Shift+a");
        await field.PressAsync("ArrowLeft");
        await field.PressAsync("Enter");
        var pressTrace = await page.EvaluateAsync<string[]>("trace");
        Assert.Contains("keydown:Control", pressTrace);
        Assert.Contains("keyup:Control", pressTrace);
        Assert.Contains("keydown:Shift", pressTrace);
        Assert.Contains("keydown:ArrowLeft", pressTrace);
        Assert.Contains("keydown:Enter", pressTrace);
        await page.EvaluateAsync("trace=[];field.value=''");
        const string multilingual = "á🙂e\u0301Жالعربيةעברית";
        await field.PressSequentiallyAsync(multilingual);
        Assert.Equal(multilingual, await page.Locator("#field").InputValueAsync());
        var typeTrace = await page.EvaluateAsync<string[]>("trace");
        Assert.Contains(typeTrace, entry => entry.StartsWith("keydown:", StringComparison.Ordinal));
        Assert.Contains(typeTrace, entry => entry.StartsWith("keyup:", StringComparison.Ordinal));
    }

    [BrowserFact]
    public async Task Trial_click_options_do_not_prepare_pointer_and_default_click_runs_once()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync("<button id='target'>go</button><script>window.moves=0;window.clicks=0;target.onmousemove=()=>moves++;target.onclick=()=>clicks++</script>");
        var wrapped = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            CompatibilityMode = Compatible,
            MouseMinimumDurationMilliseconds = 0,
            MouseMaximumDurationMilliseconds = 0
        }).Wrap(page);
        await page.Locator("#target").ClickAsync(new LocatorClickOptions { Trial = true });
        var rawTrialMoves = await page.EvaluateAsync<int>("moves");
        await page.EvaluateAsync("moves=0");
        await wrapped.Locator("#target").ClickAsync(new LocatorClickOptions { Trial = true });
        Assert.Equal(rawTrialMoves, await page.EvaluateAsync<int>("moves"));
        Assert.Equal(0, await page.EvaluateAsync<int>("clicks"));
        await wrapped.Locator("#target").ClickAsync();
        Assert.Equal(1, await page.EvaluateAsync<int>("clicks"));
        await wrapped.Locator("#target").HoverAsync();
        Assert.True(await page.EvaluateAsync<int>("moves") > 0);
    }

    [BrowserFact]
    public async Task Click_options_and_raw_routes_preserve_effects_without_mutating_options()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync("<button id='raw' style='width:120px;height:60px'>raw</button><button id='wrapped' style='width:120px;height:60px'>wrapped</button><script>window.events=[];for(const b of [raw,wrapped])for(const n of ['click','contextmenu'])b.addEventListener(n,e=>events.push([b.id,n,e.button,e.clientX,e.clientY,e.ctrlKey]))</script>");
        var wrapped = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            CompatibilityMode = Compatible,
            MouseMinimumDurationMilliseconds = 0,
            MouseMaximumDurationMilliseconds = 0
        }).Wrap(page);
        var rawOptions = new LocatorClickOptions { Position = new Position { X = 9, Y = 11 }, Modifiers = new[] { KeyboardModifier.Control }, Button = MouseButton.Right, Force = true };
        var wrappedOptions = new LocatorClickOptions { Position = new Position { X = 9, Y = 11 }, Modifiers = new[] { KeyboardModifier.Control }, Button = MouseButton.Right, Force = true };
        await page.Locator("#raw").ClickAsync(rawOptions);
        await wrapped.Locator("#wrapped").ClickAsync(wrappedOptions);
        Assert.Equal(MouseButton.Right, rawOptions.Button);
        Assert.Equal(MouseButton.Right, wrappedOptions.Button);
        Assert.Equal(new[] { KeyboardModifier.Control }, wrappedOptions.Modifiers);
        Assert.True(wrappedOptions.Force);
        Assert.Equal(new[] { 9d, 11d }, new[] { (double)wrappedOptions.Position!.X, wrappedOptions.Position.Y });
        var events = await page.EvaluateAsync<string>("JSON.stringify(window.events)");
        Assert.Contains("raw", events);
        Assert.Contains("wrapped", events);
        Assert.Contains("contextmenu", events);

        var typeOptions = new LocatorPressSequentiallyOptions { Delay = 25, Timeout = 2_000 };
        var before = (typeOptions.Delay, typeOptions.Timeout);
        await wrapped.Locator("#wrapped").PressSequentiallyAsync("xy", typeOptions);
        Assert.Equal(before, (typeOptions.Delay, typeOptions.Timeout));
    }

    [BrowserFact]
    public async Task Locator_actions_keep_one_effect_through_detach_overlay_and_link_navigation()
    {
        using var site = new LoopbackSite();
        await site.StartAsync();
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync($"<button id='target'>go</button><a id='link' href='{site.Url}navigated'>next</a><div id='overlay' style='display:none;position:fixed;inset:0'></div><script>window.clicks=0;target.onclick=()=>clicks++</script>");
        var wrapped = new PlaywrightHumanizer(new HumanInteractionOptions { CompatibilityMode = Compatible }).Wrap(page);
        await wrapped.Locator("#target").ClickAsync();
        Assert.Equal(1, await page.EvaluateAsync<int>("clicks"));
        await page.EvaluateAsync("overlay.style.display='block';setTimeout(()=>overlay.style.display='none',40)");
        await wrapped.Locator("#target").ClickAsync(new LocatorClickOptions { Timeout = 2_000 });
        Assert.Equal(2, await page.EvaluateAsync<int>("clicks"));
        var detach = page.EvaluateAsync("setTimeout(()=>target.remove(),0)");
        var detached = await Record.ExceptionAsync(() => wrapped.Locator("#target").ClickAsync(new LocatorClickOptions { Timeout = 250 }));
        await detach;
        Assert.NotNull(detached);
        Assert.Equal(2, await page.EvaluateAsync<int>("clicks"));
        await wrapped.Locator("#link").ClickAsync();
        Assert.Contains("navigated", page.Url);
    }

    [BrowserFact]
    public async Task Preparatory_pointer_endpoint_tracks_native_final_click_point()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync("<button id='target' style='position:absolute;left:180px;top:120px;width:100px;height:50px'>go</button><script>window.trace=[];target.addEventListener('mousemove',e=>trace.push(['move',e.clientX,e.clientY]));target.addEventListener('click',e=>trace.push(['click',e.clientX,e.clientY]))</script>");
        var wrapped = new PlaywrightHumanizer(new HumanInteractionOptions { CompatibilityMode = Compatible, MouseMinimumDurationMilliseconds = 0, MouseMaximumDurationMilliseconds = 0 }).Wrap(page);
        await wrapped.Locator("#target").ClickAsync();
        Assert.Equal("click", await page.EvaluateAsync<string>("trace.at(-1)[0]"));
        var points = await page.EvaluateAsync<double[][]>("trace.map(e=>[e[1],e[2]])");
        Assert.NotEmpty(points);
        Assert.InRange(Math.Abs(points[^1][0] - 230), 0, 2);
        Assert.InRange(Math.Abs(points[^1][1] - 145), 0, 2);
    }

    [BrowserFact]
    public async Task Readiness_deadline_expires_before_late_target_appears()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync("<script>window.clicks=0;setTimeout(()=>{document.body.insertAdjacentHTML('beforeend','<button id=late>late</button>');document.querySelector('#late').onclick=()=>clicks++},600)</script>");
        var wrapped = new PlaywrightHumanizer(new HumanInteractionOptions { CompatibilityMode = Compatible, ActionDeadlineMilliseconds = 150 }).Wrap(page);
        var failure = await Record.ExceptionAsync(() => wrapped.Locator("#late").ClickAsync());
        Assert.NotNull(failure);
        Assert.True(failure is TimeoutException or TaskCanceledException or PlaywrightException);
    }

    [BrowserFact]
    public async Task Focus_loss_during_type_does_not_replay_or_reinsert_the_prefix()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync("<input id='field'><script>window.inputs=0;field.addEventListener('input',()=>{inputs++;field.blur()})</script>");
        var wrapped = new PlaywrightHumanizer(new HumanInteractionOptions { CompatibilityMode = Compatible, KeyMinimumDelayMilliseconds = 0, KeyMaximumDelayMilliseconds = 0 }).Wrap(page);
        var error = await Record.ExceptionAsync(() => wrapped.Locator("#field").PressSequentiallyAsync("abc"));
        Assert.Null(error);
        Assert.Equal("a", await page.Locator("#field").InputValueAsync());
        Assert.Equal(1, await page.EvaluateAsync<int>("inputs"));
    }

    [BrowserFact]
    public async Task Typing_deadline_is_bounded_and_closing_page_stops_later_characters()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.SetContentAsync("<input id='field'><script>window.count=0;field.addEventListener('input',()=>count++)</script>");
        var humanizer = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            CompatibilityMode = Compatible,
            KeyMinimumDelayMilliseconds = 35,
            KeyMaximumDelayMilliseconds = 35,
            // This test measures the fixed cadence deadline, not stochastic thinking
            // pauses that can legitimately consume the whole budget after one key.
            ThinkingPauseProbability = 0,
            TypingDeadlineMilliseconds = 180
        });
        var wrapped = humanizer.Wrap(page);
        var field = wrapped.Locator("#field");
        await field.FocusAsync();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var failure = await Record.ExceptionAsync(() => field.PressSequentiallyAsync(new string('x', 100)));
        stopwatch.Stop();
        Assert.NotNull(failure);
        var prefixLength = await page.Locator("#field").InputValueAsync();
        Assert.InRange(prefixLength.Length, 2, 12);
        Assert.InRange(stopwatch.ElapsedMilliseconds, 100, 1_500);

        await page.EvaluateAsync("(()=>{field.value='';window.count=0;window.firstTypingInput=new Promise(resolve=>field.addEventListener('input',resolve,{once:true}));return true})()");
        await field.FocusAsync();
        var operation = field.PressSequentiallyAsync(new string('z', 500));
        await page.EvaluateAsync("window.firstTypingInput");
        await page.CloseAsync();
        var closeError = await Record.ExceptionAsync(async () => await operation);
        Assert.NotNull(closeError);
        Assert.False(operation.IsCompletedSuccessfully);
    }

    [BrowserFact]
    public async Task Public_helpers_share_the_page_gate_with_wrapped_input_and_keep_other_pages_parallel()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.SetContentAsync("<input id='field'><script>window.trace=[];field.addEventListener('input',e=>trace.push(e.data))</script>");
        var options = new HumanInteractionOptions { CompatibilityMode = Compatible, KeyMinimumDelayMilliseconds = 35, KeyMaximumDelayMilliseconds = 35 };
        var humanizer = new PlaywrightHumanizer(options);
        var wrapped = humanizer.Wrap(page);
        await wrapped.Locator("#field").FocusAsync();
        var actions = new HumanActions(options);
        var direct = actions.TypeFocusedAsync(page, "direct");
        var forwarded = wrapped.Keyboard.TypeAsync("wrapped");
        await Task.WhenAll(direct, forwarded);
        var trace = await page.EvaluateAsync<string[]>("window.trace");
        var sent = string.Concat(trace);
        Assert.True(sent is "directwrapped" or "wrappeddirect", $"Direct and wrapped helpers interleaved: {sent}");

        var otherPage = await context.NewPageAsync();
        await otherPage.SetContentAsync("<input id='field'><script>window.firstAt=0;window.firstInput=new Promise(resolve=>field.addEventListener('input',()=>{if(!firstAt)firstAt=performance.now();resolve()}, {once:true}))</script>");
        await page.EvaluateAsync("(()=>{field.value='';window.firstAt=0;window.firstInput=new Promise(resolve=>field.addEventListener('input',()=>{if(!firstAt)firstAt=performance.now();resolve()}, {once:true}));return true})()");
        await page.Locator("#field").FocusAsync();
        await otherPage.Locator("#field").FocusAsync();
        var slower = new HumanInteractionOptions { KeyMinimumDelayMilliseconds = 250, KeyMaximumDelayMilliseconds = 250, TypingDeadlineMilliseconds = 8_000 };
        var firstPageInput = new HumanActions(slower).TypeFocusedAsync(page, "abcdefgh");
        var otherPageInput = new HumanActions(slower).TypeFocusedAsync(otherPage, "abcdefgh");
        await Task.WhenAll(page.EvaluateAsync("window.firstInput"), otherPage.EvaluateAsync("window.firstInput"));
        var firstTimes = await Task.WhenAll(page.EvaluateAsync<double>("window.firstAt"), otherPage.EvaluateAsync<double>("window.firstAt"));
        Assert.InRange(Math.Abs(firstTimes[0] - firstTimes[1]), 0, 1_500);
        await Task.WhenAll(firstPageInput, otherPageInput);
    }

    [BrowserFact]
    public async Task Diagnostics_never_include_typed_secret_sentinel_and_unsupported_options_are_raw()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync("<input id='field'>");
        const string sentinel = "MATRIX_SECRET_7f31c9";
        var humanizer = new PlaywrightHumanizer(new HumanInteractionOptions { CompatibilityMode = Compatible, EnableDiagnostics = true });
        var wrapped = humanizer.Wrap(page);
        await wrapped.Locator("#field").FillAsync(sentinel);
        await wrapped.Locator("#field").PressSequentiallyAsync("safe", new LocatorPressSequentiallyOptions { Delay = 0 });
        var diagnostics = humanizer.GetDiagnosticsSnapshot();
        Assert.NotNull(diagnostics);
        Assert.DoesNotContain(sentinel, System.Text.Json.JsonSerializer.Serialize(diagnostics), StringComparison.Ordinal);
        Assert.Equal(sentinel + "safe", await page.Locator("#field").InputValueAsync());
    }

    [BrowserFact]
    public async Task Wrapped_timeout_failure_keeps_the_page_alive_and_raw_errors_are_category_compared()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var rawPage = await browser.NewPageAsync();
        var wrappedPage = await browser.NewPageAsync();
        await rawPage.SetContentAsync("<input id='hidden' style='display:none'>");
        await wrappedPage.SetContentAsync("<input id='hidden' style='display:none'>");
        var wrapped = new PlaywrightHumanizer(new HumanInteractionOptions { CompatibilityMode = Compatible }).Wrap(wrappedPage);
        var rawError = await Record.ExceptionAsync(() => rawPage.Locator("#hidden").FillAsync("x", new LocatorFillOptions { Timeout = 100 }));
        var wrappedError = await Record.ExceptionAsync(() => wrapped.Locator("#hidden").FillAsync("x", new LocatorFillOptions { Timeout = 100 }));
        Assert.NotNull(rawError);
        Assert.NotNull(wrappedError);
        Assert.True(IsTimeoutCategory(rawError!));
        Assert.True(IsTimeoutCategory(wrappedError!));
        Assert.False(wrappedPage.IsClosed, "Wrapped timeout must not close its page.");
    }

    private static bool IsTimeoutCategory(Exception error) => error is TimeoutException or TaskCanceledException or PlaywrightException;

    private static Task<string> ReadValue(ILocator locator, string id) => id == "edit"
        ? locator.EvaluateAsync<string>("element => element.innerText")
        : locator.InputValueAsync();

    [BrowserFact]
    public async Task Closing_context_while_fill_waits_observes_the_original_task()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        var wrapped = new PlaywrightHumanizer(new HumanInteractionOptions { CompatibilityMode = Compatible }).Wrap(page);
        var operation = wrapped.Locator("#never-added").FillAsync("not inserted", new LocatorFillOptions { Timeout = 10_000 });
        await Task.Delay(150);
        await context.CloseAsync();
        var error = await Record.ExceptionAsync(async () => await operation);
        Assert.NotNull(error);
        Assert.False(operation.IsCompletedSuccessfully);
    }
}
