using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using PlotboekGenerator.WebApp.Authentication;

namespace PlotboekGenerator.test.Authentication;

public sealed class ClaimRoleResolver_specs
{
    private static readonly Dictionary<string, string?> Provider = new()
    {
        ["Authentication:Oidc:Providers:0:Scheme"] = "Club",
        ["Authentication:Oidc:Providers:0:Authority"] = "https://auth.example.test",
        ["Authentication:Oidc:Providers:0:ClientId"] = "plotboek",
        ["Authentication:Oidc:Providers:0:RoleClaim"] = "roles",
        ["Authentication:Roles:Admin"] = "plot-admin",
        ["Authentication:Roles:User"] = "plot-sl",
        ["Authentication:Roles:NPC"] = "plot-npc",
    };

    [Test]
    public void An_address_listed_as_admin_is_admin()
    {
        var sut = Resolver(new() { ["Authentication:Admins"] = "boss@example.test; other@example.test" });

        sut.ResolveRole(Login("Google", email: "Boss@Example.test")).Should().Be(UserRoles.Admin, "e-mail addresses compare without case");
    }

    [Test]
    public void Admins_can_be_listed_as_an_array()
    {
        var sut = Resolver(new() { ["Authentication:Admins:0"] = "boss@example.test" });

        sut.ResolveRole(Login("Google", email: "boss@example.test")).Should().Be(UserRoles.Admin);
    }

    [Test]
    public void A_provider_without_a_role_claim_says_nothing()
        => Resolver(new()).ResolveRole(Login("Google", email: "sl@example.test"))
            .Should().BeNull("the user's role is then left as it is");

    [TestCase("plot-admin", UserRoles.Admin)]
    [TestCase("plot-sl", UserRoles.User)]
    [TestCase("plot-npc", UserRoles.NPC)]
    [TestCase("something-else", UserRoles.Guest)]
    public void A_role_claim_maps_to_a_role(string value, string role)
        => Resolver(Provider).ResolveRole(Login("Club", claims: [new Claim("roles", value)]))
            .Should().Be(role);

    [Test]
    public void The_highest_granted_role_wins()
        => Resolver(Provider).ResolveRole(Login("Club", claims: [new Claim("roles", "plot-npc"), new Claim("roles", "plot-admin")]))
            .Should().Be(UserRoles.Admin);

    [Test]
    public void Roles_in_the_id_token_count_too()
    {
        var login = Login("Club", tokens: [new AuthenticationToken { Name = "id_token", Value = Jwt(new { roles = new[] { "plot-sl" } }) }]);

        Resolver(Provider).ResolveRole(login).Should().Be(UserRoles.User, "a provider does not copy every token claim onto the identity");
    }

    [Test]
    public void A_role_as_a_single_string_in_the_access_token_counts()
    {
        var login = Login("Club", tokens: [new AuthenticationToken { Name = "access_token", Value = Jwt(new { roles = "plot-npc" }) }]);

        Resolver(Provider).ResolveRole(login).Should().Be(UserRoles.NPC);
    }

    [Test]
    public void A_malformed_token_grants_nothing()
    {
        var login = Login("Club", tokens: [new AuthenticationToken { Name = "id_token", Value = "not-a-jwt" }]);

        Resolver(Provider).ResolveRole(login).Should().Be(UserRoles.Guest);
    }

    private static ClaimRoleResolver Resolver(Dictionary<string, string?> settings)
        => new(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());

    private static ExternalLoginInfo Login(
        string provider,
        string? email = null,
        IEnumerable<Claim>? claims = null,
        IEnumerable<AuthenticationToken>? tokens = null)
    {
        var all = (claims ?? []).ToList();
        if (email is not null)
        {
            all.Add(new Claim(ClaimTypes.Email, email));
        }

        return new ExternalLoginInfo(new ClaimsPrincipal(new ClaimsIdentity(all, provider)), provider, "key", provider)
        {
            AuthenticationTokens = tokens?.ToList(),
        };
    }

    private static string Jwt(object payload)
    {
        static string Encode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        return $"{Encode("{\"alg\":\"none\"}"u8.ToArray())}.{Encode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)))}.";
    }
}
