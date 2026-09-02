namespace SpyBrowser.Core;

public sealed class IdentityInUseException : Exception
{
    public IdentityInUseException(string identityId)
        : base($"Browser identity '{identityId}' is already active in another worker.")
    {
        IdentityId = identityId;
    }

    public IdentityInUseException(string identityId, Exception innerException)
        : base($"Browser identity '{identityId}' is already active in another worker.", innerException)
    {
        IdentityId = identityId;
    }

    public string IdentityId { get; }
}
