using Microsoft.Extensions.Configuration;
using PlotboekGenerator.WebApp.Authentication;

namespace PlotboekGenerator.test.Authentication;

public sealed class OidcProviderSettings_specs
{
    [Test]
    public void No_listed_provider_reads_as_none()
        => OidcProviderSettings.ReadAll(Configuration([])).Should().BeEmpty();

    [Test]
    public void A_provider_with_only_the_required_keys_gets_the_defaults()
    {
        var provider = OidcProviderSettings.ReadAll(Configuration(new()
        {
            ["Authentication:Oidc:Providers:0:Scheme"] = "Google",
            ["Authentication:Oidc:Providers:0:Authority"] = "https://accounts.google.com",
            ["Authentication:Oidc:Providers:0:ClientId"] = "client",
        })).Single();

        provider.Should().BeEquivalentTo(new
        {
            Scheme = "Google",
            DisplayName = "Google",
            ClientSecret = string.Empty,
            Scopes = new[] { "openid", "profile", "email" },
            CallbackPath = "/signin-oidc-google",
            SignedOutCallbackPath = "/signout-callback-oidc-google",
            RoleClaim = string.Empty,
        }, because: "every provider needs a callback path of its own, so the default is derived from the scheme");
    }

    [Test]
    public void Every_key_a_provider_sets_is_kept()
    {
        var provider = OidcProviderSettings.ReadAll(Configuration(new()
        {
            ["Authentication:Oidc:Providers:0:Scheme"] = "Club",
            ["Authentication:Oidc:Providers:0:DisplayName"] = "Club account",
            ["Authentication:Oidc:Providers:0:Authority"] = "https://auth.example.test/realms/club",
            ["Authentication:Oidc:Providers:0:ClientId"] = "plotboek",
            ["Authentication:Oidc:Providers:0:ClientSecret"] = "secret",
            ["Authentication:Oidc:Providers:0:Scopes:0"] = "openid",
            ["Authentication:Oidc:Providers:0:Scopes:1"] = "roles",
            ["Authentication:Oidc:Providers:0:CallbackPath"] = "/signin-oidc",
            ["Authentication:Oidc:Providers:0:SignedOutCallbackPath"] = "/signout-callback-oidc",
            ["Authentication:Oidc:Providers:0:RoleClaim"] = "roles",
        })).Single();

        provider.Should().BeEquivalentTo(new
        {
            DisplayName = "Club account",
            ClientSecret = "secret",
            Scopes = new[] { "openid", "roles" },
            CallbackPath = "/signin-oidc",
            SignedOutCallbackPath = "/signout-callback-oidc",
            RoleClaim = "roles",
        });
    }

    [TestCase("Scheme")]
    [TestCase("Authority")]
    [TestCase("ClientId")]
    public void A_provider_missing_a_required_key_is_refused(string key)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Authentication:Oidc:Providers:0:Scheme"] = "Google",
            ["Authentication:Oidc:Providers:0:Authority"] = "https://accounts.google.com",
            ["Authentication:Oidc:Providers:0:ClientId"] = "client",
        };
        settings[$"Authentication:Oidc:Providers:0:{key}"] = "  ";

        var reading = () => OidcProviderSettings.ReadAll(Configuration(settings));

        reading.Should().Throw<InvalidOperationException>(because: "a half-filled entry is a paste that went wrong")
            .WithMessage($"*Authentication:Oidc:Providers:0:{key}*");
    }

    [Test]
    public void Two_providers_cannot_share_a_scheme()
    {
        var reading = () => OidcProviderSettings.ReadAll(Configuration(new()
        {
            ["Authentication:Oidc:Providers:0:Scheme"] = "Google",
            ["Authentication:Oidc:Providers:0:Authority"] = "https://accounts.google.com",
            ["Authentication:Oidc:Providers:0:ClientId"] = "one",
            ["Authentication:Oidc:Providers:1:Scheme"] = "google",
            ["Authentication:Oidc:Providers:1:Authority"] = "https://accounts.google.com",
            ["Authentication:Oidc:Providers:1:ClientId"] = "two",
        }));

        reading.Should().Throw<InvalidOperationException>().WithMessage("*share the scheme*");
    }

    private static IConfiguration Configuration(Dictionary<string, string?> settings)
        => new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
}
