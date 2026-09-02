using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace SpyBrowser.Core;

/// <summary>Cross-process lease that enforces one active browser per persistent profile.</summary>
public sealed class IdentityLease : IDisposable, IAsyncDisposable
{
    private static readonly ConcurrentDictionary<string, byte> ProcessLeases = new(
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

    private FileStream? _stream;
    private string? _processLeaseKey;

    private IdentityLease(string identityId, string path, FileStream stream, string processLeaseKey)
    {
        IdentityId = identityId;
        Path = path;
        _stream = stream;
        _processLeaseKey = processLeaseKey;
    }

    public string IdentityId { get; }

    public string Path { get; }

    public static async Task<IdentityLease> AcquireAsync(
        IdentityStore store,
        string identityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        var normalized = global::SpyBrowser.Core.IdentityId.Normalize(identityId);
        _ = await store.GetAsync(normalized, cancellationToken).ConfigureAwait(false);
        return await AcquirePathAsync(
            normalized,
            store.GetLeasePath(normalized),
            cancellationToken).ConfigureAwait(false);
    }

    public static async Task<IdentityLease> AcquirePathAsync(
        string identityId,
        string leasePath,
        CancellationToken cancellationToken = default)
    {
        var normalized = global::SpyBrowser.Core.IdentityId.Normalize(identityId);
        ArgumentException.ThrowIfNullOrWhiteSpace(leasePath);
        leasePath = System.IO.Path.GetFullPath(leasePath);
        var directory = System.IO.Path.GetDirectoryName(leasePath)
            ?? throw new ArgumentException("The lease path must have a parent directory.", nameof(leasePath));
        Directory.CreateDirectory(directory);

        if (!ProcessLeases.TryAdd(leasePath, 0))
        {
            throw new IdentityInUseException(normalized);
        }

        FileStream? stream = null;
        try
        {
            stream = new FileStream(
                leasePath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.WriteThrough);
            if (!OperatingSystem.IsMacOS())
            {
                stream.Lock(0, 1);
            }
        }
        catch (IOException exception)
        {
            stream?.Dispose();
            ProcessLeases.TryRemove(leasePath, out _);
            throw new IdentityInUseException(normalized, exception);
        }
        catch
        {
            stream?.Dispose();
            ProcessLeases.TryRemove(leasePath, out _);
            throw;
        }

        try
        {
            var metadata = JsonSerializer.Serialize(new
            {
                identityId = normalized,
                processId = Environment.ProcessId,
                machineName = Environment.MachineName,
                acquiredAtUtc = DateTimeOffset.UtcNow
            }, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
            var bytes = Encoding.UTF8.GetBytes(metadata + Environment.NewLine);
            stream.SetLength(0);
            await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            return new IdentityLease(normalized, leasePath, stream, leasePath);
        }
        catch
        {
            await stream.DisposeAsync().ConfigureAwait(false);
            ProcessLeases.TryRemove(leasePath, out _);
            throw;
        }
    }

    public void Dispose()
    {
        var stream = Interlocked.Exchange(ref _stream, null);
        var processLeaseKey = Interlocked.Exchange(ref _processLeaseKey, null);
        try
        {
            ReleaseFileLock(stream);
        }
        finally
        {
            stream?.Dispose();
            if (processLeaseKey is not null)
            {
                ProcessLeases.TryRemove(processLeaseKey, out _);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        var stream = Interlocked.Exchange(ref _stream, null);
        var processLeaseKey = Interlocked.Exchange(ref _processLeaseKey, null);
        try
        {
            ReleaseFileLock(stream);
            if (stream is not null)
            {
                await stream.DisposeAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            if (processLeaseKey is not null)
            {
                ProcessLeases.TryRemove(processLeaseKey, out _);
            }
        }
    }

    private static void ReleaseFileLock(FileStream? stream)
    {
        if (stream is null)
        {
            return;
        }

        try
        {
            if (!OperatingSystem.IsMacOS())
            {
                stream.Unlock(0, 1);
            }
        }
        catch (IOException)
        {
            // Closing the file descriptor below still releases an operating-system lock.
        }
    }
}
