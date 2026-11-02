namespace PlotboekGenerator.test.Pages;

public sealed class AdminModel_RoleGroups_specs
{
    private static AdminModel CreateModel()
        => new (null!, null!, null!, null!, null!, null!, null!);

    [Test]
    public void ValidateUsersExist_returns_false_when_source_null()
    {
        var model = CreateModel();
        var target = new PlotboekUser
        {
            Id = "user-2",
            Lidnummer = string.Empty,
            FirstName = string.Empty,
            LastName = string.Empty,
            CreatedAt = default,
            UpdatedAt = default,
            IsDummy = false,
            PrimaryDomainId = System.Guid.Empty,
        };

        model.ValidateUsersExist(null, target).Should().BeFalse();
        model.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Test]
    public void ValidateUsersExist_returns_false_when_target_null()
    {
        var model = CreateModel();
        var source = new PlotboekUser
        {
            Id = "user-1",
            Lidnummer = string.Empty,
            FirstName = string.Empty,
            LastName = string.Empty,
            CreatedAt = default,
            UpdatedAt = default,
            IsDummy = false,
            PrimaryDomainId = System.Guid.Empty,
        };

        model.ValidateUsersExist(source, null).Should().BeFalse();
    }

    [Test]
    public void ValidateUsersExist_returns_false_when_both_null()
    {
        var model = CreateModel();

        model.ValidateUsersExist(null, null).Should().BeFalse();
    }

    [Test]
    public void ValidateUsersExist_returns_true_when_both_present()
    {
        var model = CreateModel();
        var source = new PlotboekUser
        {
            Id = "user-1",
            Lidnummer = string.Empty,
            FirstName = string.Empty,
            LastName = string.Empty,
            CreatedAt = default,
            UpdatedAt = default,
            IsDummy = false,
            PrimaryDomainId = System.Guid.Empty,
        };
        var target = new PlotboekUser
        {
            Id = "user-2",
            Lidnummer = string.Empty,
            FirstName = string.Empty,
            LastName = string.Empty,
            CreatedAt = default,
            UpdatedAt = default,
            IsDummy = false,
            PrimaryDomainId = System.Guid.Empty,
        };

        model.ValidateUsersExist(source, target).Should().BeTrue();
    }

    [Test]
    public void InitializeUsersByRole_creates_entries_for_all_available_roles()
    {
        var model = CreateModel();

        model.InitializeUsersByRole();

        model.UsersByRole.Should().ContainKey("Admin");
        model.UsersByRole.Should().ContainKey("User");
        model.UsersByRole.Should().ContainKey("NPC");
        model.UsersByRole.Should().ContainKey("Guest");
        model.UsersByRole["Admin"].Should().BeEmpty();
        model.UsersByRole["User"].Should().BeEmpty();
        model.UsersByRole["NPC"].Should().BeEmpty();
        model.UsersByRole["Guest"].Should().BeEmpty();
    }

    [Test]
    public void AddUserToRoleGroup_places_user_in_primary_role_group()
    {
        var model = CreateModel();
        model.InitializeUsersByRole();

        var userInfo = new AdminModel.UserInfo
        {
            Id = "user-1",
            UserName = "alice",
            Email = "alice@example.com",
            FirstName = "Alice",
            LastName = "Smith",
            Roles = ["Admin"],
            ExternalLogins = [],
            CreatedAt = default,
            IsDummy = false,
        };

        model.AddUserToRoleGroup(userInfo);

        model.UsersByRole["Admin"].Should().ContainSingle();
        model.UsersByRole["Admin"][0].Id.Should().Be("user-1");
    }

    [Test]
    public void AddUserToRoleGroup_falls_back_to_Guest_when_no_roles()
    {
        var model = CreateModel();
        model.InitializeUsersByRole();

        var userInfo = new AdminModel.UserInfo
        {
            Id = "user-1",
            UserName = "bob",
            Email = "bob@example.com",
            FirstName = "Bob",
            LastName = "Jones",
            Roles = [],
            ExternalLogins = [],
            CreatedAt = default,
            IsDummy = false,
        };

        model.AddUserToRoleGroup(userInfo);

        model.UsersByRole["Guest"].Should().ContainSingle();
    }

    [Test]
    public void AddUserToRoleGroup_falls_back_to_Guest_when_role_not_in_available_roles()
    {
        var model = CreateModel();
        model.InitializeUsersByRole();

        var userInfo = new AdminModel.UserInfo
        {
            Id = "user-1",
            UserName = "charlie",
            Email = "charlie@example.com",
            FirstName = "Charlie",
            LastName = "Brown",
            Roles = ["SuperAdmin"],
            ExternalLogins = [],
            CreatedAt = default,
            IsDummy = false,
        };

        model.AddUserToRoleGroup(userInfo);

        model.UsersByRole["Guest"].Should().ContainSingle();
    }

    [Test]
    public void RemoveEmptyRoleGroups_removes_roles_with_no_users()
    {
        var model = CreateModel();
        model.InitializeUsersByRole();

        var userInfo = new AdminModel.UserInfo
        {
            Id = "user-1",
            UserName = "alice",
            Email = "alice@example.com",
            FirstName = "Alice",
            LastName = "Smith",
            Roles = ["Admin"],
            ExternalLogins = [],
            CreatedAt = default,
            IsDummy = false,
        };
        model.AddUserToRoleGroup(userInfo);

        model.RemoveEmptyRoleGroups();

        model.UsersByRole.Should().ContainKey("Admin");
        model.UsersByRole.Should().NotContainKey("User");
        model.UsersByRole.Should().NotContainKey("NPC");
        model.UsersByRole.Should().NotContainKey("Guest");
    }

    [Test]
    public void RemoveEmptyRoleGroups_keeps_all_groups_when_all_have_users()
    {
        var model = CreateModel();
        model.InitializeUsersByRole();

        model.AddUserToRoleGroup(new AdminModel.UserInfo
        {
            Id = "u1", UserName = "a", Email = string.Empty, FirstName = string.Empty, LastName = string.Empty,
            Roles = ["Admin"], ExternalLogins = [], CreatedAt = default, IsDummy = false,
        });
        model.AddUserToRoleGroup(new AdminModel.UserInfo
        {
            Id = "u2", UserName = "b", Email = string.Empty, FirstName = string.Empty, LastName = string.Empty,
            Roles = ["User"], ExternalLogins = [], CreatedAt = default, IsDummy = false,
        });
        model.AddUserToRoleGroup(new AdminModel.UserInfo
        {
            Id = "u3", UserName = "c", Email = string.Empty, FirstName = string.Empty, LastName = string.Empty,
            Roles = ["NPC"], ExternalLogins = [], CreatedAt = default, IsDummy = false,
        });
        model.AddUserToRoleGroup(new AdminModel.UserInfo
        {
            Id = "u4", UserName = "d", Email = string.Empty, FirstName = string.Empty, LastName = string.Empty,
            Roles = [], ExternalLogins = [], CreatedAt = default, IsDummy = false,
        });

        model.RemoveEmptyRoleGroups();

        model.UsersByRole.Should().HaveCount(4);
    }

    [Test]
    public void AvailableRoles_are_deterministic_with_NPC_between_User_and_Guest()
    {
        var model = CreateModel();

        model.AvailableRoles.Should().Equal("Admin", "User", "NPC", "Guest");
    }
}
