namespace PlotboekGenerator.test.Pages;

public sealed class AdminModel_UserMatchesFilter_specs
{
    private static PlotboekUser CreateUser(
        string? userName = null,
        string? email = null,
        string? firstName = "",
        string? lastName = "") => new ()
    {
        Id = Guid.NewGuid().ToString(),
        UserName = userName,
        Email = email,
        FirstName = firstName!,
        LastName = lastName!,
        Lidnummer = string.Empty,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsDummy = false,
        PrimaryDomainId = Guid.Empty,
    };

    [Test]
    public void Matches_by_username()
    {
        var user = CreateUser(userName: "TestUser");
        AdminModel.UserMatchesFilter(user, "testuser").Should().BeTrue();
    }

    [Test]
    public void Matches_by_username_case_insensitive()
    {
        var user = CreateUser(userName: "TestUser");
        AdminModel.UserMatchesFilter(user, "TESTUSER").Should().BeTrue();
    }

    [Test]
    public void Matches_by_partial_username()
    {
        var user = CreateUser(userName: "TestUser");
        AdminModel.UserMatchesFilter(user, "test").Should().BeTrue();
    }

    [Test]
    public void Matches_by_email()
    {
        var user = CreateUser(email: "test@example.com");
        AdminModel.UserMatchesFilter(user, "example").Should().BeTrue();
    }

    [Test]
    public void Matches_by_first_name()
    {
        var user = CreateUser(firstName: "Johannes");
        AdminModel.UserMatchesFilter(user, "johannes").Should().BeTrue();
    }

    [Test]
    public void Matches_by_last_name()
    {
        var user = CreateUser(lastName: "DeVries");
        AdminModel.UserMatchesFilter(user, "devries").Should().BeTrue();
    }

    [Test]
    public void No_match_returns_false()
    {
        var user = CreateUser(userName: "Jan", email: "jan@example.test", firstName: "Jan", lastName: "Bakker");
        AdminModel.UserMatchesFilter(user, "xyz").Should().BeFalse();
    }

    [Test]
    public void Null_username_does_not_throw()
    {
        var user = CreateUser(userName: null, email: "test@example.test");
        AdminModel.UserMatchesFilter(user, "test").Should().BeTrue();
    }

    [Test]
    public void Null_email_does_not_throw()
    {
        var user = CreateUser(userName: "test", email: null);
        AdminModel.UserMatchesFilter(user, "test").Should().BeTrue();
    }
}
