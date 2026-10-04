namespace SpyBrowser.Playwright;

public enum HumanizationCompatibilityMode
{
    Legacy,
    PlaywrightCompatible
}

public enum MouseTrajectoryAlgorithm
{
    Bezier,
    Cursory
}

public sealed record HumanInteractionOptions
{
    /// <summary>Selects the mouse trajectory generator. Cursory is an experimental opt-in.</summary>
    public MouseTrajectoryAlgorithm MouseAlgorithm { get; init; } = MouseTrajectoryAlgorithm.Bezier;

    /// <summary>Sampling frequency passed to the Cursory generator, in Hz.</summary>
    public double CursoryFrequency { get; init; } = 60;

    /// <summary>Sample-interval randomizer passed to Cursory; zero disables temporal jitter.</summary>
    public double CursoryFrequencyRandomizer { get; init; } = 1;

    /// <summary>Directness preference passed to Cursory, in the inclusive range [0, 1].</summary>
    public double CursoryDirectness { get; init; } = 0.65;

    /// <summary>Maximum time budget for one Cursory mouse move.</summary>
    public int CursoryMovementDeadlineMilliseconds { get; init; } = 2_000;

    public HumanizationCompatibilityMode CompatibilityMode { get; init; } = HumanizationCompatibilityMode.Legacy;

    /// <summary>Maximum total budget for a compatible action, including queue/readiness/preparation.</summary>
    public int ActionDeadlineMilliseconds { get; init; } = 30_000;

    /// <summary>Maximum total budget for one human-paced typing operation.</summary>
    public int TypingDeadlineMilliseconds { get; init; } = 30_000;

    public int MouseMinimumDurationMilliseconds { get; init; } = 180;

    public int MouseMaximumDurationMilliseconds { get; init; } = 650;

    public int KeyMinimumDelayMilliseconds { get; init; } = 35;

    public int KeyMaximumDelayMilliseconds { get; init; } = 140;

    public int ScrollMinimumSteps { get; init; } = 4;

    public int ScrollMaximumSteps { get; init; } = 12;

    public int ClickHoldMinimumMilliseconds { get; init; } = 45;

    public int ClickHoldMaximumMilliseconds { get; init; } = 130;

    public int DoubleClickIntervalMinimumMilliseconds { get; init; } = 70;

    public int DoubleClickIntervalMaximumMilliseconds { get; init; } = 170;

    public double ThinkingPauseProbability { get; init; } = 0.035;

    public int ThinkingPauseMinimumMilliseconds { get; init; } = 180;

    public int ThinkingPauseMaximumMilliseconds { get; init; } = 620;

    internal void Validate()
    {
        if (!Enum.IsDefined(MouseAlgorithm))
        {
            throw new ArgumentOutOfRangeException(nameof(MouseAlgorithm));
        }

        if (!double.IsFinite(CursoryFrequency) || CursoryFrequency is < 1 or > 120)
        {
            throw new ArgumentOutOfRangeException(nameof(CursoryFrequency));
        }

        if (!double.IsFinite(CursoryFrequencyRandomizer) || CursoryFrequencyRandomizer < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(CursoryFrequencyRandomizer));
        }

        if (!double.IsFinite(CursoryDirectness) || CursoryDirectness is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(CursoryDirectness));
        }

        if (CursoryMovementDeadlineMilliseconds <= 0 || CursoryMovementDeadlineMilliseconds > 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(CursoryMovementDeadlineMilliseconds));
        }

        if (!Enum.IsDefined(CompatibilityMode))
        {
            throw new ArgumentOutOfRangeException(nameof(CompatibilityMode));
        }

        if (ActionDeadlineMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ActionDeadlineMilliseconds));
        }

        if (TypingDeadlineMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(TypingDeadlineMilliseconds));
        }

        if (MouseMinimumDurationMilliseconds < 0 ||
            MouseMaximumDurationMilliseconds < MouseMinimumDurationMilliseconds)
        {
            throw new ArgumentOutOfRangeException(nameof(MouseMinimumDurationMilliseconds));
        }

        if (KeyMinimumDelayMilliseconds < 0 || KeyMaximumDelayMilliseconds < KeyMinimumDelayMilliseconds)
        {
            throw new ArgumentOutOfRangeException(nameof(KeyMinimumDelayMilliseconds));
        }

        if (ScrollMinimumSteps < 1 || ScrollMaximumSteps < ScrollMinimumSteps)
        {
            throw new ArgumentOutOfRangeException(nameof(ScrollMinimumSteps));
        }

        if (ClickHoldMinimumMilliseconds < 0 || ClickHoldMaximumMilliseconds < ClickHoldMinimumMilliseconds)
        {
            throw new ArgumentOutOfRangeException(nameof(ClickHoldMinimumMilliseconds));
        }

        if (DoubleClickIntervalMinimumMilliseconds < 0 ||
            DoubleClickIntervalMaximumMilliseconds < DoubleClickIntervalMinimumMilliseconds)
        {
            throw new ArgumentOutOfRangeException(nameof(DoubleClickIntervalMinimumMilliseconds));
        }

        if (ThinkingPauseProbability is < 0 or > 1 ||
            ThinkingPauseMinimumMilliseconds < 0 ||
            ThinkingPauseMaximumMilliseconds < ThinkingPauseMinimumMilliseconds)
        {
            throw new ArgumentOutOfRangeException(nameof(ThinkingPauseProbability));
        }
    }
}
