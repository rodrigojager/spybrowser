using SpyBrowser.Playwright;

namespace SpyBrowser.Tests;

public sealed class HumanActionsTests
{
    [Fact]
    public void Constructor_rejects_invalid_timing_ranges()
    {
        var options = new HumanInteractionOptions
        {
            MouseMinimumDurationMilliseconds = 500,
            MouseMaximumDurationMilliseconds = 100
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => new HumanActions(options));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Constructor_rejects_invalid_thinking_probability(double probability)
    {
        var options = new HumanInteractionOptions { ThinkingPauseProbability = probability };

        Assert.Throws<ArgumentOutOfRangeException>(() => new HumanActions(options));
    }
}
