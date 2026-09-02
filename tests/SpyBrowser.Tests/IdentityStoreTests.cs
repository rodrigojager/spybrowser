using SpyBrowser.Core;

namespace SpyBrowser.Tests;

public sealed class IdentityStoreTests
{
    [Fact]
    public async Task Create_round_trips_manifest_and_creates_profile()
    {
        using var temporary = new TemporaryDirectory();
        var store = new IdentityStore(temporary.Path);
        var identity = BrowserIdentity.Create("cliente-a", "Cliente A");

        await store.CreateAsync(identity);
        var loaded = await store.GetAsync(identity.Id);
        var listed = await store.ListAsync();

        Assert.Equal(identity, loaded);
        Assert.Single(listed);
        Assert.True(Directory.Exists(store.GetProfileDirectory(identity.Id)));
    }

    [Fact]
    public async Task Create_does_not_overwrite_existing_identity()
    {
        using var temporary = new TemporaryDirectory();
        var store = new IdentityStore(temporary.Path);
        var identity = BrowserIdentity.Create("cliente-a");
        await store.CreateAsync(identity);

        await Assert.ThrowsAsync<IOException>(() => store.CreateAsync(identity));
    }

    [Fact]
    public async Task Lease_prevents_two_active_workers_for_same_profile()
    {
        using var temporary = new TemporaryDirectory();
        var store = new IdentityStore(temporary.Path);
        var identity = BrowserIdentity.Create("cliente-a");
        await store.CreateAsync(identity);
        await using var firstLease = await IdentityLease.AcquireAsync(store, identity.Id);

        await Assert.ThrowsAsync<IdentityInUseException>(
            () => IdentityLease.AcquireAsync(store, identity.Id));
    }

    [Fact]
    public async Task Different_identities_can_be_leased_concurrently()
    {
        using var temporary = new TemporaryDirectory();
        var store = new IdentityStore(temporary.Path);
        await store.CreateAsync(BrowserIdentity.Create("cliente-a"));
        await store.CreateAsync(BrowserIdentity.Create("cliente-b"));

        await using var first = await IdentityLease.AcquireAsync(store, "cliente-a");
        await using var second = await IdentityLease.AcquireAsync(store, "cliente-b");

        Assert.Equal("cliente-a", first.IdentityId);
        Assert.Equal("cliente-b", second.IdentityId);
    }

    [Fact]
    public async Task Arbitrary_persistent_profile_path_has_an_exclusive_lease()
    {
        using var temporary = new TemporaryDirectory();
        var profile = Path.Combine(temporary.Path, "external-profile");
        var leasePath = profile + ".spybrowser.lock";

        await using var first = await IdentityLease.AcquirePathAsync("external", leasePath);

        await Assert.ThrowsAsync<IdentityInUseException>(
            () => IdentityLease.AcquirePathAsync("external", leasePath));
    }

    [Fact]
    public async Task Released_profile_path_can_be_acquired_again()
    {
        using var temporary = new TemporaryDirectory();
        var leasePath = Path.Combine(temporary.Path, "profile.spybrowser.lock");
        var first = await IdentityLease.AcquirePathAsync("reusable", leasePath);

        await first.DisposeAsync();
        await using var second = await IdentityLease.AcquirePathAsync("reusable", leasePath);

        Assert.Equal("reusable", second.IdentityId);
    }
}
