using System.Runtime.InteropServices;
using SpyBrowser.Playwright;
using SpyBrowser.Playwright.Diagnostics;

namespace SpyBrowser.Tests;

public sealed class DiagnosticSnapshotStoreTests
{
    [Fact]
    public async Task Saves_unique_private_snapshots_without_secret_sentinels_and_compares_fields()
    {
        using var directory = new TemporaryDirectory();
        var store = new DiagnosticSnapshotStore(directory.Path);
        var baseline = Snapshot("124", "baseline-not-secret") with
        {
            Findings = [new ConsistencyFinding(ConsistencySeverity.Warning, "private.detail", "baseline-not-secret")]
        };
        var first = await store.SaveAsync(baseline);
        var current = Snapshot("126", "password-sentinel-cookie-token") with
        {
            Findings = [new ConsistencyFinding(ConsistencySeverity.Warning, "private.detail", "password-sentinel-cookie-token")]
        };
        var second = await store.SaveAsync(current);

        Assert.NotEqual(first, second);
        Assert.True(File.Exists(first));
        Assert.True(File.Exists(second));
        var text = await File.ReadAllTextAsync(second);
        Assert.DoesNotContain("password-sentinel-cookie-token", text, StringComparison.Ordinal);
        Assert.DoesNotContain("baseline-not-secret", text, StringComparison.Ordinal);
        Assert.Contains("Finding details are omitted", text, StringComparison.Ordinal);
        if (!OperatingSystem.IsWindows())
        {
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(second));
        }
        var comparison = DiagnosticSnapshotStore.Compare(baseline, current);
        Assert.Contains(comparison.Changes, change => change.Field == "browser.version" && change.Severity == ConsistencySeverity.Information);
        Assert.DoesNotContain("password-sentinel-cookie-token", System.Text.Json.JsonSerializer.Serialize(comparison), StringComparison.Ordinal);
        Assert.DoesNotContain("baseline-not-secret", System.Text.Json.JsonSerializer.Serialize(comparison), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Every_serialized_string_field_is_projected_to_bounded_safe_values()
    {
        using var directory = new TemporaryDirectory();
        const string sentinel = "secret-sentinel https://private.invalid C:\\Users\\private\\failure";
        var source = Snapshot(sentinel, sentinel) with
        {
            Versions = new RuntimeVersionRecord { SpyBrowser = sentinel, Playwright = sentinel, BrowserFamily = sentinel, BrowserFamilySource = sentinel, BrowserChannel = sentinel, BrowserVersion = sentinel, BrowserVersionSource = sentinel, Algorithm = sentinel, Dataset = sentinel },
            Expectations = new ConsistencyExpectations { Locale = sentinel, TimezoneId = sentinel, UserAgent = sentinel, BrowserFamily = sentinel, Platform = sentinel },
            Characteristics = new BrowserSurfaceDiagnostics
            {
                UserAgent = sentinel, Platform = sentinel, ClientHintPlatform = sentinel, ClientHintArchitecture = sentinel,
                ClientHintModel = sentinel, ClientHintBrands = [sentinel], Languages = [sentinel], TimezoneId = sentinel,
                WebGl1 = new WebGlSurfaceDiagnostics { Vendor = sentinel, Renderer = sentinel, Version = sentinel, ShadingLanguageVersion = sentinel, CanvasSampleHash = sentinel },
                WebGl2 = new WebGlSurfaceDiagnostics { Vendor = sentinel, Renderer = sentinel, Version = sentinel, ShadingLanguageVersion = sentinel, CanvasSampleHash = sentinel },
                WebGpu = new WebGpuSurfaceDiagnostics { Vendor = sentinel, Architecture = sentinel, Device = sentinel, Description = sentinel, Error = sentinel }
            },
            Findings = [new ConsistencyFinding(ConsistencySeverity.Warning, sentinel, sentinel)]
        };
        var path = await new DiagnosticSnapshotStore(directory.Path).SaveAsync(source);
        var json = await File.ReadAllTextAsync(path);
        Assert.DoesNotContain(sentinel, json, StringComparison.Ordinal);
        var safe = await DiagnosticSnapshotStore.ReadAsync(path);
        Assert.Equal("unknown", safe.Versions.BrowserFamily);
        Assert.Equal("unknown", safe.Versions.BrowserFamilySource);
        Assert.Null(safe.Versions.BrowserChannel);
        Assert.Equal("unknown", safe.Versions.BrowserVersionSource);
        Assert.Equal("diagnostic.other", safe.Findings[0].Code);
        Assert.Null(safe.Characteristics.WebGpu.Error);
    }

    [Fact]
    public async Task Concurrent_saves_use_distinct_paths_and_retention_is_bounded()
    {
        using var directory = new TemporaryDirectory();
        var store = new DiagnosticSnapshotStore(directory.Path, retentionCount: 40);
        var secondStore = new DiagnosticSnapshotStore(directory.Path, retentionCount: 40);
        var paths = await Task.WhenAll(Enumerable.Range(0, 24).Select(index =>
            (index % 2 == 0 ? store : secondStore).SaveAsync(Snapshot("1", null))));
        Assert.Equal(paths.Length, paths.Distinct(StringComparer.Ordinal).Count());
        Assert.All(paths, path => Assert.True(File.Exists(path)));

        var limited = new DiagnosticSnapshotStore(directory.Path, retentionCount: 3);
        await limited.SaveAsync(Snapshot("2", null));
        Assert.InRange(Directory.GetFiles(directory.Path, "snapshot-*.json").Length, 1, 3);
    }

    [Fact]
    public async Task Corrupt_unknown_schema_and_cancelled_write_fail_without_replacing_baseline()
    {
        using var directory = new TemporaryDirectory();
        var store = new DiagnosticSnapshotStore(directory.Path);
        var baselinePath = Path.Combine(directory.Path, "accepted-baseline.json");
        const string baselineContent = "baseline remains byte-for-byte";
        await File.WriteAllTextAsync(baselinePath, baselineContent);
        var before = await File.ReadAllBytesAsync(baselinePath);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAsync(Snapshot("1", null), cancelled.Token));
        Assert.Equal(before, await File.ReadAllBytesAsync(baselinePath));
        Assert.Empty(Directory.GetFiles(directory.Path, "snapshot-*.json"));
        Assert.Empty(Directory.GetFiles(directory.Path, "*.tmp", SearchOption.AllDirectories));
        await Assert.ThrowsAsync<InvalidDataException>(() => DiagnosticSnapshotStore.ReadAsync(baselinePath));

        var unknownPath = Path.Combine(directory.Path, "unknown.json");
        await File.WriteAllTextAsync(unknownPath, "{\"schemaVersion\":77}");
        await Assert.ThrowsAsync<InvalidDataException>(() => DiagnosticSnapshotStore.ReadAsync(unknownPath));
    }

    [Fact]
    public async Task Retention_protects_explicit_baseline_inside_snapshot_directory()
    {
        using var directory = new TemporaryDirectory();
        var store = new DiagnosticSnapshotStore(directory.Path, retentionCount: 1);
        var baseline = await store.SaveAsync(Snapshot("1", null));
        for (var i = 0; i < 4; i++) await store.SaveAsync(Snapshot(i.ToString(), null), baseline);
        Assert.True(File.Exists(baseline));
        Assert.Equal(2, Directory.GetFiles(directory.Path, "snapshot-*.json").Length);
        await DiagnosticSnapshotStore.ReadAsync(baseline);
    }

    [Fact]
    public async Task Interrupted_midstream_and_cancelled_serialization_remove_partial_temp_and_preserve_baseline()
    {
        using var directory = new TemporaryDirectory();
        var baselinePath = Path.Combine(directory.Path, "snapshot-baseline.json");
        var baseline = new byte[] { 0, 1, 2, 255 };
        await File.WriteAllBytesAsync(baselinePath, baseline);
        var store = new DiagnosticSnapshotStore(directory.Path);
        store.WriteHookForTesting = async (stream, token) =>
        {
            await stream.WriteAsync(new byte[] { 123, 34, 120 }, token);
            throw new IOException("injected write failure");
        };
        await Assert.ThrowsAsync<IOException>(() => store.SaveAsync(Snapshot("1", null)));
        Assert.Empty(Directory.GetFiles(directory.Path, "*.tmp", SearchOption.TopDirectoryOnly));
        Assert.Equal(baseline, await File.ReadAllBytesAsync(baselinePath));

        using var cancellation = new CancellationTokenSource();
        store.WriteHookForTesting = async (stream, token) =>
        {
            await stream.WriteAsync(new byte[] { 123 }, token);
            cancellation.Cancel();
            token.ThrowIfCancellationRequested();
        };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAsync(Snapshot("2", null), cancellation.Token));
        Assert.Empty(Directory.GetFiles(directory.Path, "*.tmp", SearchOption.TopDirectoryOnly));
        Assert.Equal(baseline, await File.ReadAllBytesAsync(baselinePath));
    }

    [Fact]
    public async Task Missing_required_schema_and_data_fields_are_rejected()
    {
        using var directory = new TemporaryDirectory();
        foreach (var (name, json) in new[]
        {
            ("empty-object", "{}"),
            ("no-schema", "{\"capturedAtUtc\":\"2024-01-01T00:00:00Z\",\"versions\":{},\"expectations\":{},\"characteristics\":{},\"findings\":[]}"),
            ("no-version-data", "{\"schemaVersion\":1,\"capturedAtUtc\":\"2024-01-01T00:00:00Z\",\"versions\":{},\"expectations\":{},\"characteristics\":{},\"findings\":[]}")
        })
        {
            var path = Path.Combine(directory.Path, name + ".json");
            await File.WriteAllTextAsync(path, json);
            await Assert.ThrowsAsync<InvalidDataException>(() => DiagnosticSnapshotStore.ReadAsync(path));
        }
    }

    [Fact]
    public async Task Unix_directory_permission_denial_does_not_publish_snapshot()
    {
        if (!OperatingSystem.IsLinux() || GetEffectiveUserId() == 0) return;
        using var directory = new TemporaryDirectory();
        File.SetUnixFileMode(directory.Path, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        try
        {
            var store = new DiagnosticSnapshotStore(directory.Path);
            await Assert.ThrowsAnyAsync<UnauthorizedAccessException>(() => store.SaveAsync(Snapshot("1", null)));
            Assert.Empty(Directory.GetFiles(directory.Path));
        }
        finally
        {
            File.SetUnixFileMode(directory.Path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    [DllImport("libc", EntryPoint = "geteuid")]
    private static extern uint GetEffectiveUserId();

    [Fact]
    public async Task Destination_blocked_by_file_fails_without_altering_existing_data()
    {
        using var directory = new TemporaryDirectory();
        var blocker = Path.Combine(directory.Path, "not-a-directory");
        const string original = "preserve-this-file";
        await File.WriteAllTextAsync(blocker, original);

        var store = new DiagnosticSnapshotStore(Path.Combine(blocker, "snapshots"));
        await Assert.ThrowsAnyAsync<IOException>(() => store.SaveAsync(Snapshot("1", null)));
        Assert.Equal(original, await File.ReadAllTextAsync(blocker));
    }

    private static DiagnosticSnapshot Snapshot(string browserVersion, string? secret) => new()
    {
        Versions = new RuntimeVersionRecord { BrowserVersion = browserVersion, Playwright = "1.61.0", Algorithm = "none", Dataset = "none" },
        Characteristics = new BrowserSurfaceDiagnostics
        {
            TimezoneId = "Etc/UTC",
            UserAgent = secret is null ? "Chrome/124" : "Chrome/126",
            WebGl1 = new WebGlSurfaceDiagnostics { Available = true, Renderer = "Renderer" },
            Screen = new ScreenSurfaceDiagnostics { Width = 1920, Height = 1080, DevicePixelRatio = 1 },
            WebGpu = new WebGpuSurfaceDiagnostics { Error = secret }
        },
        Findings = Array.Empty<ConsistencyFinding>()
    };
}
