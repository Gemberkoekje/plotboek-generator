using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using PlotboekGenerator.WebApp;

namespace PlotboekGenerator.test.Integration;

/// <summary>
/// Which language a visitor reads: the switcher's cookie, then the browser, then the deployment's
/// default, and only among the languages the deployment speaks.
/// </summary>
public sealed class Localization_specs
{
    private const string Login = "/Identity/Account/Login";

    [Test]
    public async Task A_deployment_without_language_settings_speaks_dutch_whatever_the_browser_wants()
    {
        using var factory = new PlotboekWebApplicationFactory();
        using var client = factory.CreateClient();

        var html = await Get(client, Login, acceptLanguage: "en-GB");

        html.Should().Contain("<html lang=\"nl\"")
            .And.Contain($"Inloggen met {PlotboekWebApplicationFactory.OidcDisplayName}")
            .And.NotContain("/culture/", "with one language there is nothing to switch to");
    }

    [Test]
    public async Task The_browser_picks_english_when_the_deployment_speaks_it()
    {
        using var factory = Bilingual(defaultCulture: "nl");
        using var client = factory.CreateClient();

        var html = await Get(client, Login, acceptLanguage: "en-GB,en;q=0.9");

        html.Should().Contain("<html lang=\"en\"")
            .And.Contain($"Sign in with {PlotboekWebApplicationFactory.OidcDisplayName}");
    }

    [Test]
    public async Task Without_a_preference_the_default_language_is_used()
    {
        using var factory = Bilingual(defaultCulture: "en");
        using var client = factory.CreateClient();

        var html = await Get(client, Login);

        html.Should().Contain("<html lang=\"en\"").And.Contain("Sign in with");
    }

    [Test]
    public async Task The_switcher_offers_the_other_language_and_comes_back_to_the_page()
    {
        using var factory = Bilingual(defaultCulture: "nl");
        using var client = factory.CreateClient();

        var html = await Get(client, "/Privacy?x=1");

        html.Should().Contain("href=\"/culture/en?returnUrl=%2FPrivacy%3Fx%3D1\"")
            .And.NotContain("href=\"/culture/nl", "the language you read is not offered");
    }

    [Test]
    public async Task The_switcher_remembers_the_choice_over_the_browser()
    {
        using var factory = Bilingual(defaultCulture: "nl");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var switched = await client.GetAsync("/culture/en?returnUrl=%2FPrivacy");
        var cookie = switched.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(".AspNetCore.Culture=", StringComparison.Ordinal));
        var html = await Get(client, Login, acceptLanguage: "nl", cookie: cookie.Split(';')[0]);

        switched.StatusCode.Should().Be(HttpStatusCode.Redirect);
        switched.Headers.Location!.OriginalString.Should().Be("/Privacy");
        cookie.Should().Contain("uic%3Den").And.Contain("expires=", "the choice lasts beyond the browser session");
        html.Should().Contain("Sign in with", "the switcher's choice outranks the browser");
    }

    [TestCase("//evil.example/steal")]
    [TestCase("/\\evil.example")]
    [TestCase("https://evil.example")]
    [TestCase("")]
    public async Task The_switcher_never_sends_you_off_the_site(string returnUrl)
    {
        using var factory = Bilingual(defaultCulture: "nl");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync($"/culture/en?returnUrl={Uri.EscapeDataString(returnUrl)}");

        response.Headers.Location!.OriginalString.Should().Be("/");
    }

    [Test]
    public async Task The_switcher_ignores_a_language_the_deployment_does_not_speak()
    {
        using var factory = Bilingual(defaultCulture: "nl");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/culture/fr?returnUrl=%2F");

        response.Headers.Contains("Set-Cookie").Should().BeFalse();
    }

    [Test]
    public async Task The_scripts_get_their_texts_in_the_readers_language()
    {
        using var factory = Bilingual(defaultCulture: "nl");
        using var client = factory.CreateClient();
        factory.Reset();

        var html = await Get(client, "/Plotboek", acceptLanguage: "en");

        html.Should().Contain("\"NpcBewaarAlsStandaard\":\"Save as template\"");
    }

    private static PlotboekWebApplicationFactory Bilingual(string defaultCulture)
        => new (new Dictionary<string, string>
        {
            [LocalizationSettings.DefaultCultureKey] = defaultCulture,
            [LocalizationSettings.SupportedCulturesKey] = "nl,en",
        });

    private static async Task<string> Get(HttpClient client, string url, string? acceptLanguage = null, string? cookie = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (acceptLanguage is not null)
        {
            request.Headers.TryAddWithoutValidation("Accept-Language", acceptLanguage);
        }

        if (cookie is not null)
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        }

        using var response = await client.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }
}
