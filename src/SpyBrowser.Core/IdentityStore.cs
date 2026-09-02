using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpyBrowser.Core;

public sealed class IdentityStore
{
    public const string ManifestFileName = "identity.json";
    public const string ProfileDirectoryName = "profile";
    public const string LeaseFileName = "identity.lock";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public IdentityStore(string? rootDirectory = null)
    {
        RootDirectory = Path.GetFullPath(rootDirectory ?? SpyBrowserPaths.GetDefaultRoot());
        IdentitiesDirectory = Path.Combine(RootDirectory, "identities");
    }

    public string RootDirectory { get; }

    public string IdentitiesDirectory { get; }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(IdentitiesDirectory);
        return Task.CompletedTask;
    }

    public async Task<BrowserIdentity> CreateAsync(
        BrowserIdentity identity,
        CancellationToken cancellationToken = default)
    {
        IdentityValidator.ValidateAndThrow(identity);
        await InitializeAsync(cancellationToken).ConfigureAwait(false);

        var identityDirectory = GetIdentityDirectory(identity.Id);
        Directory.CreateDirectory(identityDirectory);
        Directory.CreateDirectory(Path.Combine(identityDirectory, ProfileDirectoryName));

        var manifestPath = Path.Combine(identityDirectory, ManifestFileName);
        await using var stream = new FileStream(
            manifestPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 16 * 1024,
            FileOptions.Asynchronous | FileOptions.WriteThrough);
        await JsonSerializer.SerializeAsync(stream, identity, SerializerOptions, cancellationToken)
            .ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        return identity;
    }

    public async Task<BrowserIdentity> GetAsync(
        string identityId,
        CancellationToken cancellationToken = default)
    {
        var manifestPath = GetManifestPath(identityId);
        if (!File.Exists(manifestPath))
        {
            throw new KeyNotFoundException($"Browser identity '{IdentityId.Normalize(identityId)}' was not found.");
        }

        await using var stream = new FileStream(
            manifestPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 16 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var identity = await JsonSerializer.DeserializeAsync<BrowserIdentity>(stream, SerializerOptions, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidDataException($"Identity manifest is empty: '{manifestPath}'.");
        IdentityValidator.ValidateAndThrow(identity);
        return identity;
    }

    public async Task<IReadOnlyList<BrowserIdentity>> ListAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        var identities = new List<BrowserIdentity>();
        foreach (var directory in Directory.EnumerateDirectories(IdentitiesDirectory).Order(StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var manifestPath = Path.Combine(directory, ManifestFileName);
            if (!File.Exists(manifestPath))
            {
                continue;
            }

            identities.Add(await GetAsync(Path.GetFileName(directory), cancellationToken).ConfigureAwait(false));
        }

        return identities;
    }

    public string GetIdentityDirectory(string identityId)
    {
        var normalized = IdentityId.Normalize(identityId);
        return EnsureChildPath(IdentitiesDirectory, Path.Combine(IdentitiesDirectory, normalized));
    }

    public string GetProfileDirectory(string identityId) =>
        EnsureChildPath(GetIdentityDirectory(identityId), Path.Combine(GetIdentityDirectory(identityId), ProfileDirectoryName));

    public string GetLeasePath(string identityId) =>
        EnsureChildPath(GetIdentityDirectory(identityId), Path.Combine(GetIdentityDirectory(identityId), LeaseFileName));

    public string GetManifestPath(string identityId) =>
        EnsureChildPath(GetIdentityDirectory(identityId), Path.Combine(GetIdentityDirectory(identityId), ManifestFileName));

    private static string EnsureChildPath(string parent, string candidate)
    {
        var resolvedParent = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var resolvedCandidate = Path.GetFullPath(candidate);
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!resolvedCandidate.StartsWith(resolvedParent, comparison))
        {
            throw new InvalidOperationException("Resolved identity path escaped the configured identity root.");
        }

        return resolvedCandidate;
    }
}
