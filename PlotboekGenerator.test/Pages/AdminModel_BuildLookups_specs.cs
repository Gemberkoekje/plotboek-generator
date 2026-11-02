namespace PlotboekGenerator.test.Pages;

public sealed class AdminModel_BuildLookups_specs
{
    [Test]
    public void BuildUserRolesLookup_empty_list_returns_empty_dictionary()
    {
        var result = AdminModel.BuildUserRolesLookup([]);

        result.Should().BeEmpty();
    }

    [Test]
    public void BuildUserRolesLookup_groups_by_user_id()
    {
        var roles = new List<UserRole>
        {
            new () { UserId = "user-1", RoleName = "Admin" },
            new () { UserId = "user-1", RoleName = "User" },
            new () { UserId = "user-2", RoleName = "Guest" },
        };

        var result = AdminModel.BuildUserRolesLookup(roles);

        result.Should().HaveCount(2);
        result["user-1"].Should().BeEquivalentTo("Admin", "User");
        result["user-2"].Should().BeEquivalentTo("Guest");
    }

    [Test]
    public void BuildUserRolesLookup_single_user_single_role()
    {
        var roles = new List<UserRole>
        {
            new () { UserId = "user-1", RoleName = "User" },
        };

        var result = AdminModel.BuildUserRolesLookup(roles);

        result.Should().ContainSingle();
        result["user-1"].Should().BeEquivalentTo("User");
    }

    [Test]
    public void BuildExternalLoginsLookup_empty_list_returns_empty_dictionary()
    {
        var result = AdminModel.BuildExternalLoginsLookup([]);

        result.Should().BeEmpty();
    }

    [Test]
    public void BuildExternalLoginsLookup_groups_by_user_id()
    {
        var logins = new List<UserExternalLogin>
        {
            new () { UserId = "user-1", LoginProvider = "Google", ProviderKey = "g1", Email = "alice@example.com" },
            new () { UserId = "user-1", LoginProvider = "Microsoft", ProviderKey = "m1", ProviderDisplayName = "alice@example.org" },
            new () { UserId = "user-2", LoginProvider = "Google", ProviderKey = "g2" },
        };

        var result = AdminModel.BuildExternalLoginsLookup(logins);

        result.Should().HaveCount(2);
        result["user-1"].Should().HaveCount(2);
        result["user-1"][0].Provider.Should().Be("Google");
        result["user-1"][0].Email.Should().Be("alice@example.com");
        result["user-1"][1].Provider.Should().Be("Microsoft");
        result["user-1"][1].Email.Should().Be("alice@example.org");
        result["user-2"].Should().ContainSingle();
    }

    [Test]
    public void BuildExternalLoginsLookup_uses_Unknown_when_no_email_or_display_name()
    {
        var logins = new List<UserExternalLogin>
        {
            new () { UserId = "user-1", LoginProvider = "Google", ProviderKey = "g1" },
        };

        var result = AdminModel.BuildExternalLoginsLookup(logins);

        result["user-1"][0].Email.Should().Be("Onbekend");
    }

    [Test]
    public void BuildExternalLoginsLookup_prefers_email_over_display_name()
    {
        var logins = new List<UserExternalLogin>
        {
            new () { UserId = "user-1", LoginProvider = "Google", ProviderKey = "g1", Email = "real@example.org", ProviderDisplayName = "display" },
        };

        var result = AdminModel.BuildExternalLoginsLookup(logins);

        result["user-1"][0].Email.Should().Be("real@example.org");
    }

    [Test]
    public void BuildExternalLoginsLookup_falls_back_to_display_name()
    {
        var logins = new List<UserExternalLogin>
        {
            new () { UserId = "user-1", LoginProvider = "Google", ProviderKey = "g1", ProviderDisplayName = "display" },
        };

        var result = AdminModel.BuildExternalLoginsLookup(logins);

        result["user-1"][0].Email.Should().Be("display");
    }
}
