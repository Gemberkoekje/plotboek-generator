using PlotboekGenerator.infrastructure.Services;

namespace PlotboekGenerator.test.Services;

/// <summary>Which accounts survive a demo's nightly reset.</summary>
public sealed class DemoResetService_specs
{
    [TestCase(UserRoles.Reader, null, false)]
    [TestCase(null, null, false)]
    [TestCase(UserRoles.Admin, null, true)]
    [TestCase(UserRoles.Reader, UserRoles.User, true)]
    [TestCase(null, UserRoles.Guest, true)]
    public void An_account_is_kept_when_it_is_an_admin_or_was_given_a_role_by_hand(string? providerRole, string? overrideRole, bool kept)
        => DemoResetService.IsKept(User(providerRole, overrideRole, isDummy: false)).Should().Be(kept);

    [Test]
    public void A_dummy_never_survives()
        => DemoResetService.IsKept(User(null, UserRoles.User, isDummy: true)).Should().BeFalse("the example brings its own spelleiders");

    private static PlotboekUser User(string? providerRole, string? overrideRole, bool isDummy) => new ()
    {
        Id = "user",
        Lidnummer = string.Empty,
        FirstName = string.Empty,
        LastName = string.Empty,
        CreatedAt = default,
        UpdatedAt = default,
        IsDummy = isDummy,
        PrimaryDomainId = Guid.Empty,
        ProviderRole = providerRole,
        OverrideRole = overrideRole,
    };
}
