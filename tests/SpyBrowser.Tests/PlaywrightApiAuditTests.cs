using System.Reflection;
using System.Text.Json;
using Microsoft.Playwright;
using Xunit.Abstractions;

namespace SpyBrowser.Tests;

public sealed class PlaywrightApiAuditTests(ITestOutputHelper output)
{
    private static string BaselinePath(Assembly assembly)
    {
        var version = assembly.GetName().Version!;
        return version.Major == 1 && version.Minor == 61
            ? "tools/verification/playwright-api-baseline.json"
            : $"tools/verification/playwright-api-baseline-{version.Major}.{version.Minor}.{version.Build}.json";
    }

    [Fact]
    public void Public_interface_and_options_surface_matches_approved_baseline()
    {
        var assembly = typeof(IPage).Assembly;
        output.WriteLine($"Audited Playwright assembly: {assembly.GetName().Version}");
        var current = CaptureSurface(assembly);
        var root = FindRepositoryRoot();
        var path = Path.Combine(root, BaselinePath(assembly).Replace('/', Path.DirectorySeparatorChar));
        var json = JsonSerializer.Serialize(current, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine;

        if (string.Equals(Environment.GetEnvironmentVariable("SPYBROWSER_UPDATE_PLAYWRIGHT_API_BASELINE"), "1", StringComparison.Ordinal))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, json);
            return;
        }

        Assert.True(File.Exists(path), $"Approved Playwright API baseline is missing: {path}");
        var approved = JsonSerializer.Deserialize<string[]>(File.ReadAllText(path)) ?? [];
        var added = current.Except(approved, StringComparer.Ordinal).ToArray();
        var removed = approved.Except(current, StringComparer.Ordinal).ToArray();
        Assert.True(added.Length == 0 && removed.Length == 0,
            $"Playwright {assembly.GetName().Version} API surface changed. Review additions and return types; " +
            $"new/changed: [{string.Join("; ", added)}], removed/changed: [{string.Join("; ", removed)}]. " +
            $"Approve intentionally with SPYBROWSER_UPDATE_PLAYWRIGHT_API_BASELINE=1.");
    }

    [BrowserFact]
    public async Task Reviewed_new_locator_surfaces_delegate_and_preserve_wrapping()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = BrowserTestSettings.Headless,
            Channel = BrowserTestSettings.Channel
        });
        output.WriteLine($"Playwright={typeof(IPage).Assembly.GetName().Version}; browser={browser.Version}; SDK={Environment.GetEnvironmentVariable("SPYBROWSER_DOTNET_SDK_VERSION") ?? "not provided"}; headed={!BrowserTestSettings.Headless}; channel={BrowserTestSettings.Channel ?? "bundled Chromium"}");
        var raw = await browser.NewPageAsync();
        await raw.SetContentAsync("<button class='item'>shown</button><button class='item' style='display:none'>hidden</button>");
        var humanizer = new SpyBrowser.Playwright.PlaywrightHumanizer();
        var locator = humanizer.Wrap(raw).Locator(".item");
        Assert.NotSame(locator, SpyBrowser.Playwright.PlaywrightHumanizer.Unwrap(locator));
        Assert.Equal(2, await locator.CountAsync());
        var visibleProperty = typeof(ILocator).GetProperty("Visible");
        if (visibleProperty is not null)
        {
            var visible = Assert.IsAssignableFrom<ILocator>(visibleProperty.GetValue(locator));
            Assert.NotSame(visible, SpyBrowser.Playwright.PlaywrightHumanizer.Unwrap(visible));
            Assert.Equal(1, await visible.CountAsync());
            Assert.Same(humanizer.Wrap(raw), visible.Page);
        }
        var waitMethod = typeof(ILocator).GetMethod("WaitForFunctionAsync");
        if (waitMethod is not null)
        {
            var task = Assert.IsAssignableFrom<Task>(waitMethod.Invoke(locator.First, ["element => element.textContent === 'shown'", null, null]));
            await task;
            Assert.Equal(2, await locator.CountAsync());
        }
    }

    private static string[] CaptureSurface(Assembly assembly)
    {
        return assembly.GetExportedTypes()
            .Where(type => type.Namespace == "Microsoft.Playwright")
            .SelectMany(type =>
            {
                var members = new List<string> { $"type {TypeName(type)}" };
                if (type.IsEnum)
                {
                    members.AddRange(Enum.GetNames(type).Select(name => $"enum {TypeName(type)}.{name}"));
                }

                members.AddRange(type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Where(method => !method.IsSpecialName)
                    .Select(method => $"method {TypeName(type)}.{method.Name}({string.Join(",", method.GetParameters().Select(parameter => TypeName(parameter.ParameterType) + (parameter.IsOptional ? "?" : "")))})=>{TypeName(method.ReturnType)}"));
                members.AddRange(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Select(constructor => $"ctor {TypeName(type)}({string.Join(",", constructor.GetParameters().Select(parameter => TypeName(parameter.ParameterType) + (parameter.IsOptional ? "?" : "")))})"));
                members.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Select(field => $"field {TypeName(type)}.{field.Name}=>{TypeName(field.FieldType)}"));
                members.AddRange(type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Select(property => $"property {TypeName(type)}.{property.Name}=>{TypeName(property.PropertyType)}"));
                members.AddRange(type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Select(@event => $"event {TypeName(type)}.{@event.Name}=>{TypeName(@event.EventHandlerType!)}"));
                return members;
            })
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static string TypeName(Type type)
    {
        if (type.IsByRef) return TypeName(type.GetElementType()!) + "&";
        if (type.IsArray) return TypeName(type.GetElementType()!) + "[]";
        if (type.IsGenericParameter) return "!" + type.Name;
        if (!type.IsGenericType) return type.FullName ?? type.Name;
        var definition = type.GetGenericTypeDefinition();
        return $"{definition.FullName}[{string.Join(",", type.GetGenericArguments().Select(TypeName))}]";
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SpyBrowser.sln"))) return directory.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate SpyBrowser.sln to resolve the approved Playwright API baseline.");
    }
}
