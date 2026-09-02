using System.Collections.ObjectModel;
using SpyBrowser.Core;

namespace SpyBrowser.Playwright;

/// <summary>
/// Rotates persistent launches across a fixed set of stable identities. Profile
/// leases make allocation safe when several callers race for the same identity.
/// </summary>
public sealed class IdentityRotationPool
{
    private readonly string[] _identityIds;
    private long _cursor;

    public IdentityRotationPool(IEnumerable<string> identityIds)
    {
        ArgumentNullException.ThrowIfNull(identityIds);
        _identityIds = identityIds
            .Select(IdentityId.Normalize)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (_identityIds.Length == 0)
        {
            throw new ArgumentException("An identity pool must contain at least one identity.", nameof(identityIds));
        }

        IdentityIds = new ReadOnlyCollection<string>(_identityIds);
        _cursor = Random.Shared.NextInt64(_identityIds.Length) - 1;
    }

    public IReadOnlyList<string> IdentityIds { get; }

    /// <summary>
    /// Launches the next currently available persistent identity. The factory
    /// receives the selected normalized identity id and must return its launch options.
    /// </summary>
    public async Task<SpyBrowserSession> LaunchNextAsync(
        Func<string, SpyBrowserLaunchOptions> optionsFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(optionsFactory);
        var start = (int)((ulong)Interlocked.Increment(ref _cursor) % (uint)_identityIds.Length);
        var busy = new List<string>(_identityIds.Length);

        for (var offset = 0; offset < _identityIds.Length; offset++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var identityId = _identityIds[(start + offset) % _identityIds.Length];
            var options = optionsFactory(identityId)
                ?? throw new InvalidOperationException("The identity options factory returned null.");
            if (!string.Equals(IdentityId.Normalize(options.IdentityId), identityId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"The options factory returned IdentityId '{options.IdentityId}' for pool identity '{identityId}'.",
                    nameof(optionsFactory));
            }

            try
            {
                return await SpyBrowserLauncher.LaunchPersistentContextAsync(options, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (IdentityInUseException)
            {
                busy.Add(identityId);
            }
        }

        throw new IdentityPoolExhaustedException(IdentityIds, busy);
    }
}

public sealed class IdentityPoolExhaustedException : InvalidOperationException
{
    public IdentityPoolExhaustedException(
        IReadOnlyList<string> identityIds,
        IReadOnlyList<string> busyIdentityIds)
        : base("No identity in the pool is currently available: " + string.Join(", ", busyIdentityIds))
    {
        IdentityIds = identityIds;
        BusyIdentityIds = busyIdentityIds;
    }

    public IReadOnlyList<string> IdentityIds { get; }

    public IReadOnlyList<string> BusyIdentityIds { get; }
}
