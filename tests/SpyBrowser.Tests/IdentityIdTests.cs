using SpyBrowser.Core;

namespace SpyBrowser.Tests;

public sealed class IdentityIdTests
{
    [Theory]
    [InlineData("Cliente-A", "cliente-a")]
    [InlineData("alpha_01", "alpha_01")]
    [InlineData("person.one", "person.one")]
    public void Normalize_returns_safe_stable_ids(string input, string expected)
    {
        Assert.Equal(expected, IdentityId.Normalize(input));
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("identity with spaces")]
    public void Normalize_rejects_path_traversal_and_unsafe_values(string input)
    {
        Assert.Throws<ArgumentException>(() => IdentityId.Normalize(input));
    }
}
