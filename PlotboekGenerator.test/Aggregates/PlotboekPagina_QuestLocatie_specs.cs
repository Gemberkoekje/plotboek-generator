using PlotboekGenerator.core.Events;

namespace PlotboekGenerator.test.Aggregates;

/// <summary>
/// Covers the v1 → v2 split of the locatie-encounter event: new writes emit v2 with the curated
/// questlocatie id, while replaying a v1 event (existing history) still yields a valid aggregate.
/// </summary>
public sealed class PlotboekPagina_QuestLocatie_specs
{
    private static PlotboekUser TestUser => new ()
    {
        Id = "user-1",
        Lidnummer = "12345",
        FirstName = "Test",
        LastName = "User",
        UserName = "testuser",
        Email = "test@example.test",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsDummy = false,
        PrimaryDomainId = Guid.Empty,
    };

    private static PlotboekPagina CreatePagina()
    {
        var pagina = new PlotboekPagina();
        var plotboekId = PlotboekId.Create(Year.Create(2025), EventIds.Voorjaar);
        pagina = pagina.AanPlotboekToevoegen(plotboekId, "Voorjaar 2025", TestUser).Value;
        pagina.MarkAllAsCommitted();
        return pagina;
    }

    private static PlotboekPagina.Data DataWith(LocatieEncounterData locatie) => new ()
    {
        Titel = "Test",
        Dag = Dag.Vrijdag,
        StarttijdId = StarttijdIds.Tijd1030,
        Order = 0,
        LocatieEncounter = locatie,
    };

    [Test]
    public void Aanpassen_stores_the_picked_questlocatie()
    {
        var pagina = CreatePagina();

        var result = pagina.Aanpassen(
            DataWith(LocatieEncounterData.Create(LocatieEncounter.Questveld, locatieId: "Leeuwenkuil")),
            TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.LocatieEncounter.LocatieId.Should().Be("Leeuwenkuil");
        result.Value.LocatieEncounter.Specificatie.Should().BeNull();
    }

    [Test]
    public void Aanpassen_only_writes_the_v2_event()
    {
        var pagina = CreatePagina();

        var result = pagina.Aanpassen(
            DataWith(LocatieEncounterData.Create(LocatieEncounter.Questveld, locatieId: "Leeuwenkuil")),
            TestUser);

        result.Value.Buffer.Uncommitted.Should().NotContain(e => e is PlotboekPaginaLocatieEncounterAangepast);
        result.Value.Buffer.Uncommitted.Should().Contain(e => e is PlotboekPaginaLocatieEncounterAangepastV2);
    }

    [Test]
    public void Switching_from_a_picked_locatie_to_free_text_clears_the_id()
    {
        var pagina = CreatePagina();
        pagina = pagina.Aanpassen(
            DataWith(LocatieEncounterData.Create(LocatieEncounter.Questveld, locatieId: "Leeuwenkuil")),
            TestUser).Value;

        var result = pagina.Aanpassen(
            DataWith(LocatieEncounterData.Create(LocatieEncounter.Questveld, "Achter de schuur")),
            TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.LocatieEncounter.LocatieId.Should().BeNull();
        result.Value.LocatieEncounter.Specificatie.Should().Be("Achter de schuur");
    }

    [Test]
    public void Replaying_the_v1_event_leaves_the_locatie_id_empty()
    {
        var pagina = CreatePagina();

        pagina.When(new PlotboekPaginaLocatieEncounterAangepast
        {
            Locatie = LocatieEncounter.Questveld,
            Specificatie = "Bij de grote eik",
            Who = TestUser.Id,
        });

        pagina.LocatieEncounter.Locatie.Should().Be(LocatieEncounter.Questveld);
        pagina.LocatieEncounter.Specificatie.Should().Be("Bij de grote eik");
        pagina.LocatieEncounter.LocatieId.Should().BeNull("v1 predates the curated questlocatie list");
    }

    [Test]
    public void Replaying_the_v2_event_restores_the_picked_locatie()
    {
        var pagina = CreatePagina();

        pagina.When(new PlotboekPaginaLocatieEncounterAangepastV2
        {
            Locatie = LocatieEncounter.Questveld,
            LocatieId = "Ruine",
            Who = TestUser.Id,
        });

        pagina.LocatieEncounter.HasQuestLocatieKeuze.Should().BeTrue();
        pagina.LocatieEncounter.LocatieId.Should().Be("Ruine");
    }
}
