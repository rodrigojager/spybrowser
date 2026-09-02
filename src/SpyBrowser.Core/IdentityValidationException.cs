namespace SpyBrowser.Core;

public sealed class IdentityValidationException : Exception
{
    public IdentityValidationException(IReadOnlyList<string> errors)
        : base("The browser identity is invalid:" + Environment.NewLine +
               string.Join(Environment.NewLine, errors.Select(error => $"- {error}")))
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }
}
