namespace SpyBrowser.Tests;

/// <summary>
/// Real-time cadence assertions must not compete with unrelated browser fixtures
/// inside this test assembly. The tests' own concurrent-page/input operations
/// remain concurrent, and their existing budgets and assertions are unchanged.
/// </summary>
[CollectionDefinition(TimedInputCollection.Name, DisableParallelization = true)]
public sealed class TimedInputCollection
{
    public const string Name = "Timed input lifecycle contracts";
}
