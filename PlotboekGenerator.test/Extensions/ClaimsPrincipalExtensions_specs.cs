using System.Security.Claims;
using PlotboekGenerator.WebApp.Extensions;

namespace PlotboekGenerator.test.Extensions;

public sealed class ClaimsPrincipalExtensions_specs
{
    [Test]
    public void GetEmail_returns_email_claim()
    {
        var user = CreatePrincipal(new Claim(ClaimTypes.Email, "test@example.com"));
        user.GetEmail().Should().Be("test@example.com");
    }

    [Test]
    public void GetEmail_returns_null_when_no_email_claim()
    {
        var user = CreatePrincipal();
        user.GetEmail().Should().BeNull();
    }

    [Test]
    public void GetName_returns_name_claim()
    {
        var user = CreatePrincipal(new Claim(ClaimTypes.Name, "Jan"));
        user.GetName().Should().Be("Jan");
    }

    [Test]
    public void GetName_falls_back_to_name_claim_type()
    {
        var user = CreatePrincipal(new Claim("name", "Piet"));
        user.GetName().Should().Be("Piet");
    }

    [Test]
    public void GetName_prefers_ClaimTypes_Name_over_name()
    {
        var user = CreatePrincipal(
            new Claim(ClaimTypes.Name, "Primary"),
            new Claim("name", "Fallback"));
        user.GetName().Should().Be("Primary");
    }

    [Test]
    public void GetName_returns_null_when_no_name_claim()
    {
        var user = CreatePrincipal();
        user.GetName().Should().BeNull();
    }

    [Test]
    public void GetLoginProvider_returns_provider_claim()
    {
        var user = CreatePrincipal(new Claim("LoginProvider", "Google"));
        user.GetLoginProvider().Should().Be("Google");
    }

    [Test]
    public void GetLoginProvider_returns_null_when_no_provider_claim()
    {
        var user = CreatePrincipal();
        user.GetLoginProvider().Should().BeNull();
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
    {
        var identity = claims.Length > 0
            ? new ClaimsIdentity(claims, "TestAuth")
            : new ClaimsIdentity();
        return new ClaimsPrincipal(identity);
    }
}
