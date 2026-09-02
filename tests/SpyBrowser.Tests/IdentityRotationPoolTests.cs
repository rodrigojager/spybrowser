using SpyBrowser.Playwright;

namespace SpyBrowser.Tests;

public sealed class IdentityRotationPoolTests
{
    [Fact]
    public void Constructor_normalizes_and_deduplicates_identities()
    {
        var pool = new IdentityRotationPool([" Cliente-A ", "cliente-a", "cliente-b"]);

        Assert.Equal(["cliente-a", "cliente-b"], pool.IdentityIds);
    }

    [Fact]
    public void Constructor_rejects_an_empty_pool()
    {
        Assert.Throws<ArgumentException>(() => new IdentityRotationPool([]));
    }
}
