using Marten;
using Marten.Services;
using PlotboekGenerator.WebApp;

namespace PlotboekGenerator.test.MartenDb;

/// <summary>
/// What the product's documents look like in Postgres. A deployment's data outlives any release, so
/// a stored name may only change on purpose.
/// </summary>
public sealed class DocumentMapping_specs
{
    [TestCase(typeof(PlotboekUser), "mt_doc_plotboekuser")]
    [TestCase(typeof(EventConfig), "mt_doc_eventconfig")]
    [TestCase(typeof(Plotboek.ExportData), "mt_doc_plotboek_exportdata")]
    public void A_fresh_database_names_the_tables_after_the_types(Type document, string table)
    {
        var options = new StoreOptions();
        options.Connection("Host=localhost;Database=fake");
        PlotboekApp.ConfigureDocuments(options);

        options.Storage.FindFeature(document).Objects.OfType<Weasel.Postgresql.Tables.Table>().Single().Identifier.Name.Should().Be(table);
    }

    [TestCase("PLIN")]
    [TestCase("KeycloakRole")]
    public void A_user_keeps_the_stored_names_of_renamed_properties(string storedName)
    {
        var user = new PlotboekUser
        {
            Id = "u1",
            Lidnummer = "1234",
            FirstName = "Test",
            LastName = "SL",
            CreatedAt = DateTime.UnixEpoch,
            UpdatedAt = DateTime.UnixEpoch,
            IsDummy = false,
            PrimaryDomainId = Guid.Empty,
            ProviderRole = UserRoles.User,
        };

        var json = new SystemTextJsonSerializer().ToJson(user);

        json.Should().Contain($"\"{storedName}\":", "documents written before the rename must keep loading");
    }

    [Test]
    public void A_stored_user_with_the_old_names_loads()
    {
        const string stored = """
            {"Id":"u1","UserName":"sl","PLIN":"1234","FirstName":"Test","LastName":"SL",
             "CreatedAt":"2026-01-01T00:00:00Z","UpdatedAt":"2026-01-01T00:00:00Z","IsDummy":false,
             "PrimaryDomainId":"00000000-0000-0000-0000-000000000000","KeycloakRole":"User"}
            """;

        var user = new SystemTextJsonSerializer().FromJson<PlotboekUser>(new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(stored)));

        user.Should().BeEquivalentTo(new { Lidnummer = "1234", ProviderRole = UserRoles.User });
    }
}
