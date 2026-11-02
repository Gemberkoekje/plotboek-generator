using System.Collections.Immutable;

namespace PlotboekGenerator.test.Pages;

public sealed class AdminModel_UserInfo_specs
{
    [Test]
    public void FullName_combines_first_and_last()
    {
        var info = CreateUserInfo(firstName: "Jan", lastName: "Bakker");
        info.FullName.Should().Be("Jan Bakker");
    }

    [Test]
    public void FullName_trims_when_only_first_name()
    {
        var info = CreateUserInfo(firstName: "Jan", lastName: string.Empty);
        info.FullName.Should().Be("Jan");
    }

    [Test]
    public void FullName_trims_when_only_last_name()
    {
        var info = CreateUserInfo(firstName: string.Empty, lastName: "Bakker");
        info.FullName.Should().Be("Bakker");
    }

    [Test]
    public void DisplayRoles_shows_guest_when_no_roles()
    {
        var info = CreateUserInfo(roles: []);
        info.DisplayRoles.Should().Be("Guest");
    }

    [Test]
    public void DisplayRoles_shows_single_role()
    {
        var info = CreateUserInfo(roles: ["Admin"]);
        info.DisplayRoles.Should().Be("Admin");
    }

    [Test]
    public void DisplayRoles_joins_multiple_roles()
    {
        var info = CreateUserInfo(roles: ["Admin", "User"]);
        info.DisplayRoles.Should().Be("Admin, User");
    }

    [Test]
    public void DisplayLogins_shows_none_when_empty()
    {
        var info = CreateUserInfo(logins: []);
        info.DisplayLogins.Should().Be("None");
    }

    [Test]
    public void DisplayLogins_shows_provider_names()
    {
        var info = CreateUserInfo(logins:
        [
            new AdminModel.ExternalLoginInfo { Id = "1", Provider = "Google", Email = "a@example.org" },
            new AdminModel.ExternalLoginInfo { Id = "2", Provider = "Microsoft", Email = "c@example.org" },
        ]);
        info.DisplayLogins.Should().Be("Google, Microsoft");
    }

    private static AdminModel.UserInfo CreateUserInfo(
        string firstName = "",
        string lastName = "",
        ImmutableList<string>? roles = null,
        ImmutableList<AdminModel.ExternalLoginInfo>? logins = null) => new ()
    {
        Id = "user-1",
        UserName = "testuser",
        Email = "test@example.test",
        FirstName = firstName,
        LastName = lastName,
        Roles = roles ?? [],
        ExternalLogins = logins ?? [],
        CreatedAt = DateTime.UtcNow,
        IsDummy = false,
    };
}

