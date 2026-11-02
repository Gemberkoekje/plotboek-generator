namespace PlotboekGenerator.test.Infrastructure;

public sealed class UserRoleResolver_specs
{
    private static PlotboekUser User(string? providerRole, string? overrideRole) => new()
    {
        Id = "u1",
        Lidnummer = string.Empty,
        FirstName = string.Empty,
        LastName = string.Empty,
        CreatedAt = default,
        UpdatedAt = default,
        IsDummy = false,
        PrimaryDomainId = Guid.Empty,
        ProviderRole = providerRole,
        OverrideRole = overrideRole,
    };

    [Test]
    public void Override_wins_over_the_provider_role()
        => User(providerRole: UserRoles.User, overrideRole: UserRoles.Admin)
            .EffectiveRole().Should().Be(UserRoles.Admin);

    [Test]
    public void Override_wins_even_when_it_is_lower_than_the_provider_role()
        => User(providerRole: UserRoles.User, overrideRole: UserRoles.Guest)
            .EffectiveRole().Should().Be(UserRoles.Guest);

    [Test]
    public void Falls_back_to_the_provider_role_when_no_override()
        => User(providerRole: UserRoles.User, overrideRole: null)
            .EffectiveRole().Should().Be(UserRoles.User);

    [Test]
    public void Defaults_to_guest_when_both_are_unset()
        => User(providerRole: null, overrideRole: null)
            .EffectiveRole().Should().Be(UserRoles.Guest);
}
