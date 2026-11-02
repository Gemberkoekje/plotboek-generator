using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace PlotboekGenerator.test.Integration;

/// <summary>
/// The generic host as a deployment meets it: what configuration decides (sign-in providers,
/// branding, path base, theme cookie) and what the product never shows (another LARP's words).
/// </summary>
public sealed class GenericHost_specs
{
    /// <summary>The opening <c>&lt;html&gt;</c> tag, where the theme attributes go.</summary>
    private static readonly Regex HtmlTag = new ("<html[^>]*>", RegexOptions.None, TimeSpan.FromSeconds(1));

    [Test]
    public async Task The_pages_open_without_a_path_base()
    {
        using var factory = new PlotboekWebApplicationFactory();
        using var client = factory.CreateClient();
        factory.Reset();
        SeedTestUser(factory);

        foreach (var page in new[] { "/", "/Privacy", "/Plotboek", "/Admin", "/Identity/Account/Login" })
        {
            using var response = await client.GetAsync(page);
            response.StatusCode.Should().Be(HttpStatusCode.OK, "{0} is a page of the product", page);
        }
    }

    [TestCase("/")]
    [TestCase("/Plotboek")]
    [TestCase("/Admin")]
    public async Task No_page_shows_the_words_of_another_LARP(string page)
    {
        using var factory = new PlotboekWebApplicationFactory();
        using var client = factory.CreateClient();
        factory.Reset();
        SeedTestUser(factory);

        var html = await client.GetStringAsync(page);

        Guards.ProductGenericity_specs.DeploymentWordsIn(html).Should().BeEmpty("the product's pages know no deployment");
    }

    [Test]
    public async Task The_brand_comes_from_configuration()
    {
        using var factory = new PlotboekWebApplicationFactory(new Dictionary<string, string>
        {
            ["Branding:Name"] = "De Verloren Kroon",
            ["Branding:FooterText"] = "Gemaakt door de spelleiding",
        });
        using var client = factory.CreateClient();

        var html = await client.GetStringAsync("/Privacy");

        html.Should().Contain("<title>Privacy Policy · De Verloren Kroon</title>")
            .And.Contain("Gemaakt door de spelleiding");
    }

    [Test]
    public async Task A_deployment_below_a_path_sets_the_path_base()
    {
        using var factory = new PlotboekWebApplicationFactory(new Dictionary<string, string> { ["Hosting:PathBase"] = "/plotboek" });
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/plotboek/Privacy");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("href=\"/plotboek/_content/PlotboekGenerator.WebApp/", "links carry the path base");
    }

    [Test]
    public async Task The_login_page_offers_the_configured_provider()
    {
        using var factory = new PlotboekWebApplicationFactory();
        using var client = factory.CreateClient();

        var html = await client.GetStringAsync("/Identity/Account/Login");

        html.Should().Contain($"name=\"provider\" value=\"{PlotboekWebApplicationFactory.OidcScheme}\"")
            .And.Contain($"Sign in with {PlotboekWebApplicationFactory.OidcDisplayName}");
    }

    [Test]
    public void Every_listed_provider_is_registered_in_order()
    {
        using var factory = new PlotboekWebApplicationFactory(new Dictionary<string, string>
        {
            ["Authentication:Oidc:Providers:1:Scheme"] = "Second",
            ["Authentication:Oidc:Providers:1:Authority"] = "https://second.example.test",
            ["Authentication:Oidc:Providers:1:ClientId"] = "plotboek",
        });

        var schemes = factory.Services.GetRequiredService<IAuthenticationSchemeProvider>();
        var names = schemes.GetRequestHandlerSchemesAsync().GetAwaiter().GetResult().Select(s => s.Name);

        names.Should().Equal([PlotboekWebApplicationFactory.OidcScheme, "Second"], "the login page lists them in configuration order");
    }

    [Test]
    public void Without_a_sign_in_provider_the_host_does_not_start()
        => Booting(new Dictionary<string, string>(), withSignInProvider: false)
            .Should().Throw<InvalidOperationException>(because: "there are no local accounts, so nobody could sign in")
            .WithMessage("*Authentication:Oidc:Providers*");

    [Test]
    public void A_listed_provider_without_an_authority_stops_the_host()
        => Booting(new Dictionary<string, string> { ["Authentication:Oidc:Providers:0:Authority"] = string.Empty })
            .Should().Throw<InvalidOperationException>(because: "a half-filled entry is a paste that went wrong, not a provider switched off")
            .WithMessage("*Authentication:Oidc:Providers:0:Authority*");

    [TestCase("light")]
    [TestCase("dark")]
    public async Task The_theme_cookie_is_the_configured_one(string theme)
    {
        using var factory = new PlotboekWebApplicationFactory(new Dictionary<string, string> { ["Theme:CookieName"] = "kroon-theme" });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/Privacy");
        request.Headers.TryAddWithoutValidation("Cookie", $"kroon-theme={theme}");

        using var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        HtmlTag.Match(html).Value.Should().Be($"<html lang=\"nl\" data-bs-theme=\"{theme}\" data-theme-cookie=\"kroon-theme\" data-theme-domain=\"\">");
    }

    [TestCase("/_content/PlotboekGenerator.WebApp/lib/bootstrap/dist/css/bootstrap.min.css", "text/css")]
    [TestCase("/_content/PlotboekGenerator.WebApp/js/plotboek-theme.js", "text/javascript")]
    [TestCase("/_content/PlotboekGenerator.WebApp/images/favicon.svg", "image/svg+xml")]
    public async Task The_product_serves_its_own_look(string path, string mediaType)
    {
        using var factory = new PlotboekWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be(mediaType);
    }

    private static Action Booting(IReadOnlyDictionary<string, string> settings, bool withSignInProvider = true)
        => () =>
        {
            using var factory = new PlotboekWebApplicationFactory(settings, withSignInProvider);
            _ = factory.Services;
        };

    private static void SeedTestUser(PlotboekWebApplicationFactory factory)
    {
        factory.TestStore.Seed(new PlotboekUser
        {
            Id = PlotboekWebApplicationFactory.TestUserId,
            UserName = PlotboekWebApplicationFactory.TestUserName,
            Email = "sl@example.test",
            Lidnummer = string.Empty,
            FirstName = "Test",
            LastName = "SL",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDummy = false,
            PrimaryDomainId = Guid.Empty,
        });
        factory.TestStore.Seed(new UserRole { UserId = PlotboekWebApplicationFactory.TestUserId, RoleName = UserRoles.Admin });
    }
}
