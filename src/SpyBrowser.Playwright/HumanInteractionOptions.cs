namespace SpyBrowser.Playwright;

public enum HumanizationCompatibilityMode
{
    Legacy,
    PlaywrightCompatible
}

public sealed record HumanInteractionOptions
{
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
