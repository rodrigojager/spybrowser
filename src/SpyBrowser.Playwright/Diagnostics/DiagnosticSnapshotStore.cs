using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpyBrowser.Playwright.Diagnostics;

public sealed record DiagnosticSnapshot
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public DateTimeOffset CapturedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public RuntimeVersionRecord Versions { get; init; } = new();
    public ConsistencyExpectations Expectations { get; init; } = new();
    public BrowserSurfaceDiagnostics Characteristics { get; init; } = new();
    public IReadOnlyList<ConsistencyFinding> Findings { get; init; } = Array.Empty<ConsistencyFinding>();
}

public sealed record DiagnosticSnapshotChange(string Field, string Before, string After, ConsistencySeverity Severity);

public sealed record DiagnosticSnapshotComparison(IReadOnlyList<DiagnosticSnapshotChange> Changes);

/// <summary>Opt-in local JSON persistence. Snapshot names are session-unique; no file is designated or replaced as a baseline.</summary>
public sealed class DiagnosticSnapshotStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly string _directory;
    private readonly int _retentionCount;
    internal Func<Stream, CancellationToken, Task>? WriteHookForTesting { get; set; }

    public DiagnosticSnapshotStore(string directory, int retentionCount = 20)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        if (retentionCount < 1) throw new ArgumentOutOfRangeException(nameof(retentionCount));
        _directory = Path.GetFullPath(directory);
        _retentionCount = retentionCount;
    }

    public string DirectoryPath => _directory;

    public async Task<string> SaveAsync(DiagnosticSnapshot snapshot, CancellationToken cancellationToken = default) =>
        await SaveAsync(snapshot, protectedBaselinePath: null, cancellationToken).ConfigureAwait(false);

    /// <summary>Saves a unique snapshot while excluding the explicitly selected baseline from retention.</summary>
    public async Task<string> SaveAsync(DiagnosticSnapshot snapshot, string? protectedBaselinePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.SchemaVersion != DiagnosticSnapshot.CurrentSchemaVersion)
            throw new InvalidDataException($"Unsupported diagnostic snapshot schema {snapshot.SchemaVersion}.");

        Directory.CreateDirectory(_directory);
        var name = $"snapshot-{DateTimeOffset.UtcNow:yyyyMMdd'T'HHmmss.fffffff'Z'}-{Guid.NewGuid():N}.json";
        var target = Path.Combine(_directory, name);
        var temporary = Path.Combine(_directory, $".{name}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                             16 * 1024, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                RestrictPermissions(temporary);
                if (WriteHookForTesting is { } writeHook)
                    await writeHook(stream, cancellationToken).ConfigureAwait(false);
                else
                    await JsonSerializer.SerializeAsync(stream, Sanitize(snapshot), JsonOptions, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }
            cancellationToken.ThrowIfCancellationRequested();
            await using (await AcquireRetentionLockAsync(cancellationToken).ConfigureAwait(false))
            {
                File.Move(temporary, target, overwrite: false);
                RestrictPermissions(target);
                ApplyRetention(target, protectedBaselinePath);
            }
            return target;
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public static async Task<DiagnosticSnapshot> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                16 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !Has(root, "schemaVersion", JsonValueKind.Number) ||
                !Has(root, "capturedAtUtc", JsonValueKind.String) ||
                !Has(root, "versions", JsonValueKind.Object) ||
                !Has(root, "expectations", JsonValueKind.Object) ||
                !Has(root, "characteristics", JsonValueKind.Object) ||
                !Has(root, "findings", JsonValueKind.Array))
                throw new InvalidDataException("The diagnostic snapshot is missing required schema or data fields.");
            var versions = Property(root, "versions");
            foreach (var field in new[] { "spyBrowser", "playwright", "browserFamily", "browserVersion", "algorithm", "dataset" })
                if (!Has(versions, field, JsonValueKind.String))
                    throw new InvalidDataException($"The diagnostic snapshot is missing required versions.{field} data.");
            if (!root.GetProperty("schemaVersion").TryGetInt32(out _))
                throw new InvalidDataException("The diagnostic snapshot schemaVersion must be an integer.");

            var snapshot = JsonSerializer.Deserialize<DiagnosticSnapshot>(root.GetRawText(), JsonOptions);
            if (snapshot is null) throw new InvalidDataException("The diagnostic snapshot is empty.");
            if (snapshot.SchemaVersion != DiagnosticSnapshot.CurrentSchemaVersion)
                throw new InvalidDataException($"Unsupported diagnostic snapshot schema {snapshot.SchemaVersion}.");
            if (snapshot.Versions is null || snapshot.Expectations is null || snapshot.Characteristics is null || snapshot.Findings is null)
                throw new InvalidDataException("The diagnostic snapshot is missing required data.");
            return snapshot;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The diagnostic snapshot is corrupt or invalid JSON.", exception);
        }
    }

    public static DiagnosticSnapshotComparison Compare(DiagnosticSnapshot baseline, DiagnosticSnapshot current)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(current);
        if (baseline.SchemaVersion != DiagnosticSnapshot.CurrentSchemaVersion || current.SchemaVersion != DiagnosticSnapshot.CurrentSchemaVersion)
            throw new InvalidDataException("Cannot compare an unsupported diagnostic snapshot schema.");
        var changes = new List<DiagnosticSnapshotChange>();
        Add("spybrowser.version", baseline.Versions.SpyBrowser, current.Versions.SpyBrowser, ConsistencySeverity.Information);
        Add("browser.family", baseline.Versions.BrowserFamily, current.Versions.BrowserFamily, ConsistencySeverity.Information);
        Add("browser.version", baseline.Versions.BrowserVersion, current.Versions.BrowserVersion, ConsistencySeverity.Information);
        Add("playwright.version", baseline.Versions.Playwright, current.Versions.Playwright, ConsistencySeverity.Information);
        Add("algorithm", baseline.Versions.Algorithm, current.Versions.Algorithm, ConsistencySeverity.Information);
        Add("dataset", baseline.Versions.Dataset, current.Versions.Dataset, ConsistencySeverity.Information);
        Add("timezone", baseline.Characteristics.TimezoneId, current.Characteristics.TimezoneId, ConsistencySeverity.Warning);
        Add("locale", string.Join(',', baseline.Characteristics.Languages), string.Join(',', current.Characteristics.Languages), ConsistencySeverity.Warning);
        Add("user-agent", baseline.Characteristics.UserAgent, current.Characteristics.UserAgent, ConsistencySeverity.Information);
        Add("platform", baseline.Characteristics.Platform, current.Characteristics.Platform, ConsistencySeverity.Information);
        Add("screen", Screen(baseline), Screen(current), ConsistencySeverity.Information);
        Add("webgl1.renderer", baseline.Characteristics.WebGl1.Renderer, current.Characteristics.WebGl1.Renderer, ConsistencySeverity.Information);
        Add("webgl2.renderer", baseline.Characteristics.WebGl2.Renderer, current.Characteristics.WebGl2.Renderer, ConsistencySeverity.Information);
        Add("webgpu.device", baseline.Characteristics.WebGpu.Device, current.Characteristics.WebGpu.Device, ConsistencySeverity.Information);
        var beforeFindings = string.Join(',', baseline.Findings.Select(f => $"{f.Code}:{f.Severity}").OrderBy(x => x));
        var afterFindings = string.Join(',', current.Findings.Select(f => $"{f.Code}:{f.Severity}").OrderBy(x => x));
        Add("findings", beforeFindings, afterFindings, current.Findings.Any(f => f.Severity == ConsistencySeverity.Error)
            ? ConsistencySeverity.Error : ConsistencySeverity.Information);
        return new DiagnosticSnapshotComparison(changes);

        void Add(string field, string? before, string? after, ConsistencySeverity severity)
        {
            before ??= "unavailable";
            after ??= "unavailable";
            if (!string.Equals(before, after, StringComparison.Ordinal))
                changes.Add(new DiagnosticSnapshotChange(field, before, after, severity));
        }
    }

    private static bool Has(JsonElement parent, string name, JsonValueKind kind) =>
        parent.TryGetProperty(name, out var value) && value.ValueKind == kind;

    private static JsonElement Property(JsonElement parent, string name) => parent.TryGetProperty(name, out var value)
        ? value : default;

    private static DiagnosticSnapshot Sanitize(DiagnosticSnapshot snapshot) => snapshot with
    {
        Characteristics = snapshot.Characteristics with
        {
            WebGl1 = snapshot.Characteristics.WebGl1 with { CanvasSampleHash = null },
            WebGl2 = snapshot.Characteristics.WebGl2 with { CanvasSampleHash = null },
            WebGpu = snapshot.Characteristics.WebGpu with { Error = null }
        },
        Findings = snapshot.Findings.Select(finding => finding with
        {
            Message = "Finding details are omitted from snapshots; use the stable code and severity."
        }).ToArray()
    };

    private async Task<FileStream> AcquireRetentionLockAsync(CancellationToken cancellationToken)
    {
        var lockPath = Path.Combine(_directory, ".snapshot-retention.lock");
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try { return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) { await Task.Delay(25, cancellationToken).ConfigureAwait(false); }
        }
    }

    private void ApplyRetention(string newest, string? protectedBaselinePath)
    {
        var protectedPath = protectedBaselinePath is null ? null : Path.GetFullPath(protectedBaselinePath);
        var files = new DirectoryInfo(_directory).GetFiles("snapshot-*.json")
            .OrderByDescending(file => file.LastWriteTimeUtc).ToArray();
        var retained = 0;
        foreach (var file in files)
        {
            if (PathEquals(file.FullName, protectedPath)) continue;
            if (retained++ < _retentionCount || PathEquals(file.FullName, newest)) continue;
            file.Delete();
        }
    }

    private static bool PathEquals(string path, string? other) => other is not null &&
        string.Equals(Path.GetFullPath(path), other, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private static string Screen(DiagnosticSnapshot snapshot) =>
        $"{snapshot.Characteristics.Screen.Width}x{snapshot.Characteristics.Screen.Height}@{snapshot.Characteristics.Screen.DevicePixelRatio}";

    private static void RestrictPermissions(string path)
    {
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
}
