namespace PlotboekGenerator.test.Pages;

public sealed class AdminModel_Validation_specs
{
    private static AdminModel CreateModel()
        => new (null!, null!, null!, null!, null!, null!, null!);

    [Test]
    public void ValidateEditUserSelected_returns_false_when_empty()
    {
        var model = CreateModel();
        model.EditUserId = null;

        model.ValidateEditUserSelected().Should().BeFalse();
        model.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Test]
    public void ValidateEditUserSelected_returns_false_when_empty_string()
    {
        var model = CreateModel();
        model.EditUserId = string.Empty;

        model.ValidateEditUserSelected().Should().BeFalse();
    }

    [Test]
    public void ValidateEditUserSelected_returns_true_when_set()
    {
        var model = CreateModel();
        model.EditUserId = "user-1";

        model.ValidateEditUserSelected().Should().BeTrue();
    }

    [Test]
    public void ValidateLinkAccountsParameters_returns_false_when_source_empty()
    {
        var model = CreateModel();
        model.SourceUserId = null;
        model.TargetUserId = "user-2";

        model.ValidateLinkAccountsParameters().Should().BeFalse();
        model.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Test]
    public void ValidateLinkAccountsParameters_returns_false_when_target_empty()
    {
        var model = CreateModel();
        model.SourceUserId = "user-1";
        model.TargetUserId = null;

        model.ValidateLinkAccountsParameters().Should().BeFalse();
    }

    [Test]
    public void ValidateLinkAccountsParameters_returns_false_when_same_user()
    {
        var model = CreateModel();
        model.SourceUserId = "user-1";
        model.TargetUserId = "user-1";

        model.ValidateLinkAccountsParameters().Should().BeFalse();
        model.ErrorMessage.Should().Contain("different");
    }

    [Test]
    public void ValidateLinkAccountsParameters_returns_true_when_valid()
    {
        var model = CreateModel();
        model.SourceUserId = "user-1";
        model.TargetUserId = "user-2";

        model.ValidateLinkAccountsParameters().Should().BeTrue();
    }

    [Test]
    public void ValidateExternalLoginExists_returns_false_when_null()
    {
        var model = CreateModel();

        model.ValidateExternalLoginExists(null).Should().BeFalse();
        model.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Test]
    public void ValidateExternalLoginExists_returns_true_when_present()
    {
        var model = CreateModel();
        var login = new UserExternalLogin
        {
            UserId = "user-1",
            LoginProvider = "Google",
            ProviderKey = "key-1",
        };

        model.ValidateExternalLoginExists(login).Should().BeTrue();
    }

    [Test]
    public void ValidateUnlinkLoginParameters_returns_false_when_empty()
    {
        var model = CreateModel();
        model.LoginIdToUnlink = null;

        model.ValidateUnlinkLoginParameters().Should().BeFalse();
        model.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Test]
    public void ValidateUnlinkLoginParameters_returns_true_when_set()
    {
        var model = CreateModel();
        model.LoginIdToUnlink = "login-1";

        model.ValidateUnlinkLoginParameters().Should().BeTrue();
    }

    [Test]
    public void SourceHasLogins_returns_false_when_empty()
    {
        AdminModel.SourceHasLogins([]).Should().BeFalse();
    }

    [Test]
    public void SourceHasLogins_returns_true_when_has_logins()
    {
        var logins = new List<UserExternalLogin>
        {
            new ()
            {
                UserId = "user-1",
                LoginProvider = "Google",
                ProviderKey = "key-1",
            },
        };

        AdminModel.SourceHasLogins(logins).Should().BeTrue();
    }
}
