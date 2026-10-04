using Microsoft.Playwright;
using SpyBrowser.Playwright;
using SpyBrowser.Playwright.Humanization;

namespace SpyBrowser.Tests;

[Collection(TimedInputCollection.Name)]
public sealed class ActionsCompatibilityTests
{
    [Fact]
    public void Bezier_strategy_produces_inspectable_path_ending_at_requested_target()
    {
        var options = new HumanInteractionOptions { MouseMinimumDurationMilliseconds = 40, MouseMaximumDurationMilliseconds = 100 };
        var path = new BezierTrajectoryStrategy().Create(5, 7, 105, 57, new Random(12), options);
        Assert.True(path.Count >= 8);
        Assert.Equal(105, path[^1].X, 6);
        Assert.Equal(57, path[^1].Y, 6);
        Assert.All(path, point => Assert.True(point.DelayMilliseconds > 0));
    }

    [Fact]
    public void Compatibility_mode_is_opt_in_and_validated()
    {
        Assert.Equal(HumanizationCompatibilityMode.Legacy, new HumanInteractionOptions().CompatibilityMode);
        Assert.Throws<ArgumentOutOfRangeException>(() => new HumanActions(new HumanInteractionOptions { TypingDeadlineMilliseconds = 0 }));
    }

    [BrowserFact]
    public async Task Compatible_actions_preserve_native_fill_insert_press_click_and_unicode_type()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync("""
            <input id="value"><button id="target">go</button>
            <script>
            window.events=[];
            const input=document.querySelector('#value');
            for (const name of ['keydown','keyup','input','change']) input.addEventListener(name,e=>events.push(name));
            window.clicks=0; window.dbl=0;
            const target=document.querySelector('#target');
            target.addEventListener('click',e=>{clicks++; target.dataset.detail=e.detail});
            target.addEventListener('dblclick',e=>dbl++);
            target.addEventListener('mousemove',()=>{if(!target.dataset.shifted){target.dataset.shifted='yes';target.style.marginLeft='60px';}});
            </script>
            """);
        var humanizer = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible,
            MouseMinimumDurationMilliseconds = 0,
            MouseMaximumDurationMilliseconds = 0,
            KeyMinimumDelayMilliseconds = 0,
            KeyMaximumDelayMilliseconds = 0
        });
        var wrapped = humanizer.Wrap(page);
        var input = wrapped.Locator("#value");
        await page.EvaluateAsync("events=[]");
        await input.FillAsync("native");
        Assert.Equal("native", await page.Locator("#value").InputValueAsync());
        await page.EvaluateAsync("events=[]");
        await input.ClearAsync();
        Assert.Equal(string.Empty, await page.Locator("#value").InputValueAsync());
        await page.EvaluateAsync("events=[]");
        await input.FillAsync("native");
        Assert.DoesNotContain("keydown", await page.EvaluateAsync<string[]>("events"));
        await input.PressAsync("Control+A");
        await input.PressAsync("Backspace");
        await input.PressSequentiallyAsync("é🙂e\u0301Ж");
        Assert.Equal("é🙂e\u0301Ж", await page.Locator("#value").InputValueAsync());
        await input.PressAsync("Control+A");
        await page.EvaluateAsync("events=[]");
        await wrapped.Keyboard.InsertTextAsync("inserted");
        Assert.Equal("inserted", await page.Locator("#value").InputValueAsync());
        Assert.DoesNotContain("keydown", await page.EvaluateAsync<string[]>("events"));
        await wrapped.Locator("#target").DblClickAsync();
        Assert.Equal("yes", await page.Locator("#target").GetAttributeAsync("data-shifted"));
        Assert.Equal(2, await page.EvaluateAsync<int>("clicks"));
        Assert.Equal(1, await page.EvaluateAsync<int>("dbl"));
        Assert.Equal("2", await page.Locator("#target").GetAttributeAsync("data-detail"));
    }

    [BrowserFact]
    public async Task Public_compatible_humanactions_delegate_clicks_and_fill_to_native_locator_actions()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync("<input id='value'><button id='b'>go</button><script>window.clicks=[];b.onclick=e=>clicks.push(e.detail);b.ondblclick=()=>window.doubles=(window.doubles||0)+1;window.keys=0;value.onkeydown=()=>keys++;</script>");
        var actions = new HumanActions(new HumanInteractionOptions
        {
            CompatibilityMode = HumanizationCompatibilityMode.PlaywrightCompatible,
            MouseMinimumDurationMilliseconds = 0,
            MouseMaximumDurationMilliseconds = 0
        });
        var value = page.Locator("#value");
        await actions.TypeAsync(value, "native-value");
        Assert.Equal("native-value", await value.InputValueAsync());
        Assert.Equal(0, await page.EvaluateAsync<int>("keys"));
        await actions.DoubleClickAsync(page.Locator("#b"));
        Assert.Equal(new[] { 1, 2 }, await page.EvaluateAsync<int[]>("clicks"));
        Assert.Equal(1, await page.EvaluateAsync<int>("doubles"));
    }
}
