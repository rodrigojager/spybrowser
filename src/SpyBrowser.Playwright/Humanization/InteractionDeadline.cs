namespace SpyBrowser.Playwright.Humanization;

/// <summary>A monotonic, total budget for SDK-owned pauses and subsequent input steps.</summary>
internal sealed class InteractionDeadline : IDisposable
{
    private readonly CancellationTokenSource _source;

    public InteractionDeadline(TimeSpan budget, CancellationToken cancellationToken)
    {
        if (budget <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(budget));
        _source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _source.CancelAfter(budget);
    }

    public CancellationToken Token => _source.Token;

    public void Dispose() => _source.Dispose();
}
