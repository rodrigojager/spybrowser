using System.Reflection;
using Microsoft.Playwright;
using SpyBrowser.Playwright;
using Xunit.Abstractions;

namespace SpyBrowser.Tests;

public sealed class DiagnosticsResourceContractTests
{
    private readonly ITestOutputHelper _output;

    public DiagnosticsResourceContractTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void Disabled_recorder_has_no_storage_and_allocation_comparison_is_observable()
    {
        const int warmupCalls = 512;
        const int measuredCalls = 8_192;
        const int boundedCapacity = 32;

        var disabledRaw = DispatchProxy.Create<IPage, HumanizationDiagnosticsTests.FakePage>();
        var disabled = new PlaywrightHumanizer();
        var disabledPage = disabled.Wrap(disabledRaw);
        _ = MeasureCurrentThreadAllocations(disabledPage, warmupCalls);
        var disabledBytes = MeasureCurrentThreadAllocations(disabledPage, measuredCalls);

        var enabledRaw = DispatchProxy.Create<IPage, HumanizationDiagnosticsTests.FakePage>();
        var enabled = new PlaywrightHumanizer(new HumanInteractionOptions
        {
            EnableDiagnostics = true,
            DiagnosticsCapacity = boundedCapacity
        });
        var enabledPage = enabled.Wrap(enabledRaw);
        _ = MeasureCurrentThreadAllocations(enabledPage, warmupCalls);
        var enabledBytes = MeasureCurrentThreadAllocations(enabledPage, measuredCalls);

        Assert.Null(disabled.GetDiagnosticsSnapshot());
        Assert.Null(disabled.Scope.DiagnosticsForTesting);
        Assert.Equal(boundedCapacity, enabled.GetDiagnosticsSnapshot()!.Records.Count);
        Assert.Equal(warmupCalls + measuredCalls - boundedCapacity, enabled.GetDiagnosticsSnapshot()!.DroppedRecords);
        _output.WriteLine("Same-thread allocations over {0} warmed TitleAsync wrapper calls: disabled={1} bytes ({2:F2}/call), enabled={3} bytes ({4:F2}/call); recorder capacity={5}, retained={6}, dropped={7}.",
            measuredCalls, disabledBytes, disabledBytes / (double)measuredCalls, enabledBytes,
            enabledBytes / (double)measuredCalls, boundedCapacity,
            enabled.GetDiagnosticsSnapshot()!.Records.Count, enabled.GetDiagnosticsSnapshot()!.DroppedRecords);
        Assert.True(enabledBytes > disabledBytes,
            $"Expected enabled recorder allocations to exceed disabled wrapper baseline; disabled={disabledBytes} bytes ({disabledBytes / (double)measuredCalls:F2}/call), enabled={enabledBytes} bytes ({enabledBytes / (double)measuredCalls:F2}/call), warmup={warmupCalls}, measured={measuredCalls}.");
    }

    private static long MeasureCurrentThreadAllocations(IPage page, int calls)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < calls; i++) page.TitleAsync().GetAwaiter().GetResult();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
