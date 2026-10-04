using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Playwright;

namespace SpyBrowser.Playwright;

/// <summary>A privacy-safe, bounded snapshot of observed Playwright wrapper calls.</summary>
public sealed record HumanizationDiagnosticsSnapshot(
    string SchemaVersion,
    string SessionId,
    HumanizationDiagnosticsProvenance Provenance,
    IReadOnlyList<HumanizationDiagnosticRecord> Records,
    int Capacity,
    long DroppedRecords);

/// <summary>Runtime provenance that does not include browser profile or user data.</summary>
public sealed record HumanizationDiagnosticsProvenance(
    string SpyBrowserVersion,
    string PlaywrightVersion,
    string BrowserVersion,
    string Algorithm,
    string DatasetVersion);

/// <summary>One observed invocation; fields are allowlisted and contain no arguments or exception text.</summary>
public sealed record HumanizationDiagnosticRecord(
    string SessionId,
    string PageId,
    string ActionId,
    string Method,
    string Mode,
    string Algorithm,
    string Outcome,
    string Reason,
    double DurationMilliseconds,
    DateTimeOffset CompletedAtUtc)
{
    public double? PlannedDurationMilliseconds { get; init; }
    public int? PlannedPoints { get; init; }
    public int? DispatchedPoints { get; init; }
    public int? CoalescedPoints { get; init; }
    public bool? FinalEndpointReached { get; init; }
}

internal sealed class HumanizationDiagnosticsRecorder
{
    private static readonly AsyncLocal<Invocation?> CurrentInvocation = new();

    internal static void ReportPlannedMovement(int count, double durationMilliseconds) => CurrentInvocation.Value?.SetPlannedMovement(count, durationMilliseconds);
    internal static void ReportMovementProgress(int dispatched, int coalesced) => CurrentInvocation.Value?.SetMovementProgress(dispatched, coalesced);
    internal static void ReportFinalEndpointReached(bool reached) => CurrentInvocation.Value?.SetFinalEndpointReached(reached);

    private readonly object _gate = new();
    private readonly HumanizationDiagnosticRecord?[] _records;
    private readonly ConditionalWeakTable<IPage, PageCorrelation> _pages = new();
    private int _next;
    private int _count;
    private long _dropped;
    private readonly string _sessionId = Guid.NewGuid().ToString("N");
    private readonly HumanizationDiagnosticsProvenance _provenance = CreateProvenance();
    private string _browserVersion = "unavailable";

    internal HumanizationDiagnosticsRecorder(int capacity) => _records = new HumanizationDiagnosticRecord[capacity];

    internal Invocation Begin(IPage? page, string method, string mode, string reason, string algorithm)
    {
        var pageId = page is null ? "unscoped" : _pages.GetValue(page, _ => new PageCorrelation()).Id;
        if (page is not null)
        {
            try
            {
                var browserVersion = page.Context.Browser?.Version;
                if (!string.IsNullOrWhiteSpace(browserVersion)) Interlocked.CompareExchange(ref _browserVersion, browserVersion, "unavailable");
            }
            catch { /* Provenance is best-effort; diagnostics must not affect automation. */ }
        }
        return new Invocation(this, Stopwatch.GetTimestamp(), pageId, method, mode, reason, algorithm, Guid.NewGuid().ToString("N"));
    }

    internal HumanizationDiagnosticsSnapshot Snapshot()
    {
        lock (_gate)
        {
            var result = new HumanizationDiagnosticRecord[_count];
            var start = (_next - _count + _records.Length) % _records.Length;
            for (var i = 0; i < _count; i++) result[i] = _records[(start + i) % _records.Length]!;
            var provenance = _provenance with { BrowserVersion = Volatile.Read(ref _browserVersion) };
            return new HumanizationDiagnosticsSnapshot("1", _sessionId, provenance,
                new ReadOnlyCollection<HumanizationDiagnosticRecord>(result), _records.Length, _dropped);
        }
    }

    private void Complete(Invocation invocation, string outcome)
    {
        var duration = (Stopwatch.GetTimestamp() - invocation.Started) * 1000d / Stopwatch.Frequency;
        var item = new HumanizationDiagnosticRecord(_sessionId, invocation.PageId, invocation.ActionId,
            invocation.Method, invocation.Mode, invocation.Algorithm, outcome, invocation.Reason,
            Math.Max(0, duration), DateTimeOffset.UtcNow)
        {
            PlannedDurationMilliseconds = invocation.PlannedDurationMilliseconds,
            PlannedPoints = invocation.PlannedPoints,
            DispatchedPoints = invocation.DispatchedPoints,
            CoalescedPoints = invocation.CoalescedPoints,
            FinalEndpointReached = invocation.FinalEndpointReached
        };
        lock (_gate)
        {
            if (_count == _records.Length) _dropped++;
            else _count++;
            _records[_next] = item;
            _next = (_next + 1) % _records.Length;
        }
    }

    private static HumanizationDiagnosticsProvenance CreateProvenance()
    {
        var asm = typeof(PlaywrightHumanizer).Assembly.GetName();
        var playwright = typeof(IPage).Assembly.GetName();
        return new HumanizationDiagnosticsProvenance(
            asm.Version?.ToString() ?? "unknown",
            playwright.Version?.ToString() ?? "unknown",
            "unavailable",
            "bezier:legacy-v1;cursory:upstream-16fff97fab05bb6b0c6753b2dc136a7692634cec",
            "cursory-js-16fff97fab05bb6b0c6753b2dc136a7692634cec:2356");
    }

    private sealed class PageCorrelation { internal string Id { get; } = Guid.NewGuid().ToString("N"); }

    internal sealed class Invocation
    {
        private readonly HumanizationDiagnosticsRecorder _owner;
        private int _completed;
        internal long Started { get; }
        internal string PageId { get; }
        internal string Method { get; }
        internal string Mode { get; }
        internal string Reason { get; }
        internal string Algorithm { get; }
        internal string ActionId { get; }
        internal double? PlannedDurationMilliseconds { get; private set; }
        internal int? PlannedPoints { get; private set; }
        internal int? DispatchedPoints { get; private set; }
        internal int? CoalescedPoints { get; private set; }
        internal bool? FinalEndpointReached { get; private set; }

        internal Invocation(HumanizationDiagnosticsRecorder owner, long started, string pageId,
            string method, string mode, string reason, string algorithm, string actionId)
        {
            _owner = owner; Started = started; PageId = pageId; Method = method;
            Mode = mode; Reason = reason; Algorithm = algorithm; ActionId = actionId;
        }

        internal IDisposable Activate()
        {
            var previous = CurrentInvocation.Value;
            CurrentInvocation.Value = this;
            return new RestoreInvocation(previous);
        }

        internal void SetPlannedMovement(int count, double durationMilliseconds)
        {
            PlannedPoints = count;
            PlannedDurationMilliseconds = durationMilliseconds;
        }
        internal void SetMovementProgress(int dispatched, int coalesced)
        {
            DispatchedPoints = dispatched;
            CoalescedPoints = coalesced;
        }
        internal void SetFinalEndpointReached(bool reached) => FinalEndpointReached = reached;

        internal void Complete(string outcome)
        {
            if (Interlocked.Exchange(ref _completed, 1) != 0) return;
            try { _owner.Complete(this, outcome); }
            catch { /* Diagnostics are strictly best-effort and cannot change the wrapped operation. */ }
        }

        private sealed class RestoreInvocation(Invocation? previous) : IDisposable
        {
            public void Dispose() => CurrentInvocation.Value = previous;
        }
    }
}
