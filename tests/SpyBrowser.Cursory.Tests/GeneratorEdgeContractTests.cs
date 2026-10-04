using Xunit;

namespace SpyBrowser.Cursory.Tests;

public sealed class GeneratorEdgeContractTests
{
    [Theory]
    [InlineData(-9.5, -4.25, 10.75, -1.5)]
    [InlineData(10.75, -1.5, -9.5, -4.25)]
    [InlineData(-3.125, 8.5, 1.25, -6.75)]
    [InlineData(1.25, -6.75, -3.125, 8.5)]
    [InlineData(4.5, -20.25, 4.5, 13.75)]
    [InlineData(-7.125, 2.25, 11.5, 2.25)]
    public void Quadrants_axes_and_fractional_coordinates_keep_exact_endpoints(
        double startX, double startY, double endX, double endY)
    {
        var start = new TrajectoryPoint(startX, startY);
        var end = new TrajectoryPoint(endX, endY);
        var result = CursoryTrajectoryGenerator.Generate(start, end, new() { Seed = UInt128.MaxValue });

        Assert.Equal(start, result.Points[0]);
        Assert.Equal(end, result.Points[^1]);
        Assert.Equal(result.Points.Count, result.Timings.Count);
        Assert.All(result.Points, point => Assert.True(double.IsFinite(point.X) && double.IsFinite(point.Y)));
        Assert.All(result.Timings.Zip(result.Timings.Skip(1)), pair => Assert.True(pair.Second >= pair.First));
    }

    [Fact]
    public void Subpixel_move_preserves_distinct_endpoints_and_zero_distance_is_noop()
    {
        var start = new TrajectoryPoint(0.125, -0.25);
        var end = new TrajectoryPoint(0.375, -0.125);
        var tiny = CursoryTrajectoryGenerator.Generate(start, end, new() { Seed = 0 });
        Assert.Equal(start, tiny.Points[0]);
        Assert.Equal(end, tiny.Points[^1]);

        var stationary = CursoryTrajectoryGenerator.Generate(start, start, new()
        {
            Seed = UInt128.MaxValue,
            Frequency = 60,
        });
        Assert.Equal(new[] { start }, stationary.Points);
        Assert.Equal(new[] { 0d }, stationary.Timings);
    }

    [Fact]
    public void Returned_point_and_timing_collections_reject_mutation_and_remain_stable()
    {
        var trajectory = CursoryTrajectoryGenerator.Generate(new(-2.5, 7.25), new(80.125, -4.75), new() { Seed = 321 });
        var pointsBefore = trajectory.Points.ToArray();
        var timingsBefore = trajectory.Timings.ToArray();

        Assert.True(Assert.IsAssignableFrom<IList<TrajectoryPoint>>(trajectory.Points).IsReadOnly);
        Assert.True(Assert.IsAssignableFrom<IList<double>>(trajectory.Timings).IsReadOnly);
        Assert.Throws<NotSupportedException>(() => ((IList<TrajectoryPoint>)trajectory.Points)[0] = new(0, 0));
        Assert.Throws<NotSupportedException>(() => ((IList<double>)trajectory.Timings)[0] = 999);
        Assert.Equal(pointsBefore, trajectory.Points);
        Assert.Equal(timingsBefore, trajectory.Timings);
    }

    [Theory]
    [InlineData(double.NaN, 60, 1)]
    [InlineData(double.PositiveInfinity, 60, 1)]
    [InlineData(double.NegativeInfinity, 60, 1)]
    [InlineData(0, 60, 1)]
    [InlineData(-1, 60, 1)]
    [InlineData(10_001, 60, 1)]
    [InlineData(60, double.NaN, 1)]
    [InlineData(60, -1, 1)]
    [InlineData(60, 10_001, 1)]
    [InlineData(60, 1, double.NaN)]
    [InlineData(60, 1, -0.01)]
    [InlineData(60, 1, 1.01)]
    public void Invalid_parameters_fail_for_non_degenerate_input(double frequency, double randomizer, double directness)
    {
        var options = new TrajectoryOptions { Frequency = frequency, FrequencyRandomizer = randomizer, Directness = directness, Seed = 42 };
        Assert.Throws<ArgumentOutOfRangeException>(() => CursoryTrajectoryGenerator.Generate(new(0, 0), new(1, 1), options));
    }

    [Fact]
    public void Maximum_seed_is_repeatable_and_repeated_generation_does_not_change_prior_result()
    {
        var seed = new TrajectoryOptions { Seed = UInt128.MaxValue };
        var first = CursoryTrajectoryGenerator.Generate(new(1.125, 2.25), new(-90.5, 45.75), seed);
        var expectedPoints = first.Points.ToArray();
        var expectedTimings = first.Timings.ToArray();
        for (int i = 0; i < 8; i++)
            _ = CursoryTrajectoryGenerator.Generate(new(i, -i), new(40 + i, 80 - i), seed);
        Assert.Equal(expectedPoints, first.Points);
        Assert.Equal(expectedTimings, first.Timings);
    }
}
