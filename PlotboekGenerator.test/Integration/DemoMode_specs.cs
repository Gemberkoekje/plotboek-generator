using System.Net;
using System.Net.Http;
using PlotboekGenerator.test.TestHelpers;
using PlotboekGenerator.WebApp;

namespace PlotboekGenerator.test.Integration;

/// <summary>
/// The generic host run as a public demo: it starts with its example, says on every page that it is
/// a demo, lets a Reader look at everything, and puts the example back when the reset is called.
/// </summary>
public sealed class DemoMode_specs
{
    private const string ResetKey = "reset-key";

    [Test]
    public async Task A_demo_starts_with_its_example()
    {
        using var factory = Demo();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");

        factory.TestStore.GetAll<Plotboek.ExportData>().Should().ContainSingle().Which.Titel.Should().Be("De Nevelkroon");
        factory.TestStore.GetAll<PlotboekPagina.ExportData>().Should().HaveCount(7);
    }

    [Test]
    public async Task Every_page_says_it_is_a_demo_and_where_to_ask_for_write_access()
    {
        using var factory = Demo();
        using var client = factory.CreateClient();

        var html = await client.GetStringAsync("/Privacy");

        html.Should().Contain("Dit is een demo.")
            .And.Contain("href=\"mailto:beheer@example.test\"", "visitors ask the operator for write access")
            .And.Contain("verdwijnen de accounts", "the privacy page says what the reset removes");
    }

    [Test]
    public async Task A_deployment_that_is_no_demo_has_no_banner_and_no_reset()
    {
        using var factory = new PlotboekWebApplicationFactory();
        using var client = factory.CreateClient();

        var html = await client.GetStringAsync("/Privacy");
        using var reset = await PostReset(client, ResetKey);

        html.Should().NotContain("Dit is een demo.");
        reset.StatusCode.Should().BeOneOf([HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed], "only a demo maps the reset");
    }

    [TestCase("", ResetKey, HttpStatusCode.ServiceUnavailable)]
    [TestCase(ResetKey, "wrong-key", HttpStatusCode.Unauthorized)]
    [TestCase(ResetKey, "", HttpStatusCode.Unauthorized)]
    public async Task The_reset_needs_its_key(string configuredKey, string providedKey, HttpStatusCode expected)
    {
        using var factory = Demo(configuredKey);
        using var client = factory.CreateClient();
        factory.TestStore.Seed(Visitor());

        using var response = await PostReset(client, providedKey);

        response.StatusCode.Should().Be(expected);
        factory.TestStore.GetAll<PlotboekUser>().Should().Contain(u => u.Id == Visitor().Id, "a refused reset touches nothing");
    }

    [Test]
    public async Task The_reset_puts_the_example_back_and_keeps_whoever_was_granted_access()
    {
        using var factory = Demo();
        using var client = factory.CreateClient();
        factory.TestStore.Seed(Visitor(), Account("editor", providerRole: UserRoles.Reader, overrideRole: UserRoles.User), Account("admin", providerRole: UserRoles.Admin));
        factory.TestStore.Seed(
            new UserRole { UserId = Visitor().Id, RoleName = UserRoles.Reader },
            new UserRole { UserId = "editor", RoleName = UserRoles.User });
        factory.TestStore.Seed(new UserExternalLogin { UserId = Visitor().Id, LoginProvider = "Google", ProviderKey = "visitor" });
        factory.TestStore.Seed(new Plotboek.ExportData
        {
            PlotboekId = PlotboekId.Create(Year.Create(2030), "Zomer"),
            Jaar = Year.Create(2030),
            EventId = "Zomer",
            Titel = "Door een bezoeker begonnen",
        });

        using var response = await PostReset(client, ResetKey);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("\"removedAccounts\":1");
        factory.TestStore.GetAll<Plotboek.ExportData>().Should().ContainSingle().Which.Titel.Should().Be("De Nevelkroon");
        factory.TestStore.GetAll<PlotboekPagina.ExportData>().Should().HaveCount(7);
        factory.EventStore.StreamCount.Should().Be(8, "the old streams are wiped and the example's written again");
        factory.TestStore.GetAll<PlotboekUser>().Where(u => !u.IsDummy).Select(u => u.Id).Should().BeEquivalentTo("editor", "admin");
        factory.TestStore.GetAll<UserRole>().Should().NotContain(r => r.UserId == Visitor().Id);
        factory.TestStore.GetAll<UserExternalLogin>().Should().BeEmpty("the visitor's login went with the account");
    }

    [Test]
    public async Task A_reader_opens_the_plotboek_that_is_not_finished()
    {
        using var factory = Demo();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, UserRoles.Reader);

        var html = await client.GetStringAsync("/Plotboek");

        html.Should().Contain("De Nevelkroon").And.Contain("Het verzegelde archief");
    }

    [Test]
    public async Task An_npc_does_not()
    {
        using var factory = Demo();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, UserRoles.NPC);

        var html = await client.GetStringAsync("/Plotboek");

        html.Should().Contain("nog geen plotboek volledig klaar").And.NotContain("Het verzegelde archief");
    }

    private static PlotboekWebApplicationFactory Demo(string resetKey = ResetKey)
        => new (new Dictionary<string, string>
        {
            ["Demo:Enabled"] = "true",
            [DemoMode.ResetApiKeyKey] = resetKey,
            ["Branding:ContactUrl"] = "mailto:beheer@example.test",
        });

    private static async Task<HttpResponseMessage> PostReset(HttpClient client, string key)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/demo/reset");
        request.Headers.Add("X-Api-Key", key);
        return await client.SendAsync(request);
    }

    private static PlotboekUser Visitor() => Account("visitor", providerRole: UserRoles.Reader);

    private static PlotboekUser Account(string id, string providerRole, string? overrideRole = null) => new ()
    {
        Id = id,
        UserName = $"{id}@example.test",
        Email = $"{id}@example.test",
        Lidnummer = string.Empty,
        FirstName = id,
        LastName = string.Empty,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsDummy = false,
        PrimaryDomainId = Guid.Empty,
        ProviderRole = providerRole,
        OverrideRole = overrideRole,
    };
}
