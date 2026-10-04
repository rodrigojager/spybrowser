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

    [Fact]
    public void Mouse_algorithm_defaults_to_legacy_bezier_and_native_options_are_validated()
    {
        Assert.Equal(MouseTrajectoryAlgorithm.Bezier, new HumanInteractionOptions().MouseAlgorithm);
        Assert.Throws<ArgumentOutOfRangeException>(() => new HumanActions(new HumanInteractionOptions { CursoryFrequency = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HumanActions(new HumanInteractionOptions { CursoryFrequencyRandomizer = double.NaN }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HumanActions(new HumanInteractionOptions { CursoryDirectness = 1.01 }));
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
