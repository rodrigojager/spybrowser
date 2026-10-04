using System.Reflection;
using System.Text.Json;
using Microsoft.Playwright;

namespace SpyBrowser.Tests;

public sealed class PlaywrightApiAuditTests
{
    private const string BaselinePath = "tools/verification/playwright-api-baseline.json";

    [Fact]
    public void Public_interface_and_options_surface_matches_approved_baseline()
    {
        var assembly = typeof(IPage).Assembly;
        var current = CaptureSurface(assembly);
        var root = FindRepositoryRoot();
        var path = Path.Combine(root, BaselinePath.Replace('/', Path.DirectorySeparatorChar));
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
