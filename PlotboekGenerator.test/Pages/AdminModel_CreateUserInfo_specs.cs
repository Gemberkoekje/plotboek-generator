namespace PlotboekGenerator.test.Pages;

public sealed class AdminModel_CreateUserInfo_specs
{
    private static PlotboekUser CreateUser(
        string id = "user-1",
        string? userName = "alice",
        string? email = "alice@example.com",
        string firstName = "Alice",
        string lastName = "Smith",
        bool isDummy = false,
        Guid? primaryDomainId = null) => new ()
    {
        Id = id,
        UserName = userName,
        Email = email,
        Lidnummer = string.Empty,
        FirstName = firstName,
        LastName = lastName,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        IsDummy = isDummy,
        PrimaryDomainId = primaryDomainId ?? Guid.Empty,
    };

    [Test]
    public void Maps_basic_user_fields()
    {
        var user = CreateUser();
        var rolesLookup = new Dictionary<string, List<string>>();
        var loginsLookup = new Dictionary<string, List<AdminModel.ExternalLoginInfo>>();
        var domainLookup = new Dictionary<Guid, string>();

        var result = AdminModel.CreateUserInfo(user, rolesLookup, loginsLookup, domainLookup);

        result.Id.Should().Be("user-1");
        result.UserName.Should().Be("alice");
        result.Email.Should().Be("alice@example.com");
        result.FirstName.Should().Be("Alice");
        result.LastName.Should().Be("Smith");
        result.IsDummy.Should().BeFalse();
    }

    [Test]
    public void Resolves_roles_from_lookup()
    {
        var user = CreateUser();
        var rolesLookup = new Dictionary<string, List<string>>
        {
            ["user-1"] = ["Admin", "User"],
        };
        var loginsLookup = new Dictionary<string, List<AdminModel.ExternalLoginInfo>>();
        var domainLookup = new Dictionary<Guid, string>();

        var result = AdminModel.CreateUserInfo(user, rolesLookup, loginsLookup, domainLookup);

        result.Roles.Should().BeEquivalentTo("Admin", "User");
    }

    [Test]
    public void Returns_empty_roles_when_user_not_in_lookup()
    {
        var user = CreateUser();
        var rolesLookup = new Dictionary<string, List<string>>();
        var loginsLookup = new Dictionary<string, List<AdminModel.ExternalLoginInfo>>();
        var domainLookup = new Dictionary<Guid, string>();

        var result = AdminModel.CreateUserInfo(user, rolesLookup, loginsLookup, domainLookup);

        result.Roles.Should().BeEmpty();
    }

    [Test]
    public void Resolves_external_logins_from_lookup()
    {
        var user = CreateUser();
        var rolesLookup = new Dictionary<string, List<string>>();
        var loginsLookup = new Dictionary<string, List<AdminModel.ExternalLoginInfo>>
        {
            ["user-1"] = [new AdminModel.ExternalLoginInfo { Id = "login-1", Provider = "Google", Email = "a@example.org" }],
        };
        var domainLookup = new Dictionary<Guid, string>();

        var result = AdminModel.CreateUserInfo(user, rolesLookup, loginsLookup, domainLookup);

        result.ExternalLogins.Should().ContainSingle();
        result.ExternalLogins[0].Provider.Should().Be("Google");
    }

    [Test]
    public void Returns_empty_logins_when_user_not_in_lookup()
    {
        var user = CreateUser();
        var rolesLookup = new Dictionary<string, List<string>>();
        var loginsLookup = new Dictionary<string, List<AdminModel.ExternalLoginInfo>>();
        var domainLookup = new Dictionary<Guid, string>();

        var result = AdminModel.CreateUserInfo(user, rolesLookup, loginsLookup, domainLookup);

        result.ExternalLogins.Should().BeEmpty();
    }

    [Test]
    public void Resolves_primary_domain_name()
    {
        var domainId = Guid.NewGuid();
        var user = CreateUser(primaryDomainId: domainId);
        var rolesLookup = new Dictionary<string, List<string>>();
        var loginsLookup = new Dictionary<string, List<AdminModel.ExternalLoginInfo>>();
        var domainLookup = new Dictionary<Guid, string>
        {
            [domainId] = "Chaos (C)",
        };

        var result = AdminModel.CreateUserInfo(user, rolesLookup, loginsLookup, domainLookup);

        result.PrimaryDomainId.Should().Be(domainId);
        result.PrimaryDomainDisplayName.Should().Be("Chaos (C)");
    }

    [Test]
    public void Domain_name_is_null_when_no_domain()
    {
        var user = CreateUser();
        var rolesLookup = new Dictionary<string, List<string>>();
        var loginsLookup = new Dictionary<string, List<AdminModel.ExternalLoginInfo>>();
        var domainLookup = new Dictionary<Guid, string>();

        var result = AdminModel.CreateUserInfo(user, rolesLookup, loginsLookup, domainLookup);

        result.PrimaryDomainDisplayName.Should().BeNull();
    }

    [Test]
    public void Domain_name_is_null_when_domain_not_found()
    {
        var user = CreateUser(primaryDomainId: Guid.NewGuid());
        var rolesLookup = new Dictionary<string, List<string>>();
        var loginsLookup = new Dictionary<string, List<AdminModel.ExternalLoginInfo>>();
        var domainLookup = new Dictionary<Guid, string>();

        var result = AdminModel.CreateUserInfo(user, rolesLookup, loginsLookup, domainLookup);

        result.PrimaryDomainDisplayName.Should().BeNull();
    }

    [Test]
    public void Handles_null_UserName()
    {
        var user = CreateUser(userName: null);
        var rolesLookup = new Dictionary<string, List<string>>();
        var loginsLookup = new Dictionary<string, List<AdminModel.ExternalLoginInfo>>();
        var domainLookup = new Dictionary<Guid, string>();

        var result = AdminModel.CreateUserInfo(user, rolesLookup, loginsLookup, domainLookup);

        result.UserName.Should().BeEmpty();
    }

    [Test]
    public void Handles_null_Email()
    {
        var user = CreateUser(email: null);
        var rolesLookup = new Dictionary<string, List<string>>();
        var loginsLookup = new Dictionary<string, List<AdminModel.ExternalLoginInfo>>();
        var domainLookup = new Dictionary<Guid, string>();

        var result = AdminModel.CreateUserInfo(user, rolesLookup, loginsLookup, domainLookup);

        result.Email.Should().BeEmpty();
    }

    [Test]
    public void Maps_IsDummy_flag()
    {
        var user = CreateUser(isDummy: true);
        var rolesLookup = new Dictionary<string, List<string>>();
        var loginsLookup = new Dictionary<string, List<AdminModel.ExternalLoginInfo>>();
        var domainLookup = new Dictionary<Guid, string>();

        var result = AdminModel.CreateUserInfo(user, rolesLookup, loginsLookup, domainLookup);

        result.IsDummy.Should().BeTrue();
    }

    [Test]
    public void Maps_CreatedAt()
    {
        var user = CreateUser();
        var rolesLookup = new Dictionary<string, List<string>>();
        var loginsLookup = new Dictionary<string, List<AdminModel.ExternalLoginInfo>>();
        var domainLookup = new Dictionary<Guid, string>();

        var result = AdminModel.CreateUserInfo(user, rolesLookup, loginsLookup, domainLookup);

        result.CreatedAt.Should().Be(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }
}
