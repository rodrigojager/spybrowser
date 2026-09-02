using System.Globalization;

namespace SpyBrowser.Cli;

internal sealed class CommandLine
{
    private readonly Dictionary<string, string?> _options = new(StringComparer.OrdinalIgnoreCase);

    public CommandLine(IEnumerable<string> arguments)
    {
        var values = arguments.ToArray();
        var positionals = new List<string>();
        for (var index = 0; index < values.Length; index++)
        {
            var value = values[index];
            if (!value.StartsWith("--", StringComparison.Ordinal))
            {
                positionals.Add(value);
                continue;
            }

            var key = value[2..];
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Invalid empty option name.");
            }

            string? optionValue = null;
            if (index + 1 < values.Length && !values[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                optionValue = values[++index];
            }

            if (!_options.TryAdd(key, optionValue))
            {
                throw new ArgumentException($"Option '--{key}' was specified more than once.");
            }
        }

        Positionals = positionals;
    }

    public IReadOnlyList<string> Positionals { get; }

    public bool Has(string name) => _options.ContainsKey(name);

    public string? Get(string name) => _options.GetValueOrDefault(name);

    public string GetRequired(string name) =>
        Get(name) ?? throw new ArgumentException($"Option '--{name}' requires a value.");

    public int GetInt(string name, int defaultValue) =>
        Get(name) is { } value
            ? int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture)
            : defaultValue;

    public float GetFloat(string name, float defaultValue) =>
        Get(name) is { } value
            ? float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture)
            : defaultValue;
}
