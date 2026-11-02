using System.Text.Json;
using PlotboekGenerator.core.Events;

namespace PlotboekGenerator.test.Aggregates;

public sealed class PlotboekPagina_Aanpassen_ExtraFields_specs
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

    [Test]
    public void Aanpassen_updates_aantal_blokken()
    {
        var pagina = CreatePagina();
        var data = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
            AantalBlokkenId = "TweeBlokken",
        };

        var result = pagina.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.AantalBlokkenId.Should().Be("TweeBlokken");
    }

    [Test]
    public void Aanpassen_updates_locatie_encounter()
    {
        var pagina = CreatePagina();
        var locatie = LocatieEncounterData.Create(LocatieEncounter.Questveld, "Bij de grote eik");
        var data = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
            LocatieEncounter = locatie,
        };

        var result = pagina.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.LocatieEncounter.Locatie.Should().Be(LocatieEncounter.Questveld);
        result.Value.LocatieEncounter.Specificatie.Should().Be("Bij de grote eik");
    }

    [Test]
    public void Aanpassen_updates_aantal_npcs()
    {
        var pagina = CreatePagina();
        var aantalNpcs = new AantalNpcsData
        {
            AlleNpcs = false,
            Rollen = [NpcRolData.Create("guard", 2, 4)],
        };
        var data = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
            AantalNpcs = aantalNpcs,
        };

        var result = pagina.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.AantalNpcs.Rollen.Should().ContainSingle();
        result.Value.AantalNpcs.TotaalMinimum.Should().Be(2);
    }

    [Test]
    public void Aanpassen_updates_geen_npcs_nodig()
    {
        var pagina = CreatePagina();
        var data = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
            AantalNpcs = AantalNpcsData.Create([], alleNpcs: false, geenNpcsNodig: true),
        };

        var result = pagina.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.AantalNpcs.GeenNpcsNodig.Should().BeTrue();
        result.Value.AantalNpcs.AlleNpcs.Should().BeFalse();
        result.Value.AantalNpcs.Rollen.Should().BeEmpty();
        // Counts as fully-specified NPC info so the pagina can be marked "volledig klaar".
        result.Value.AantalNpcs.HasValue.Should().BeTrue();
    }

    // Covers the aggregate half of the round trip only: command -> event -> When(...) -> state. The store
    // it runs against (InMemoryEventStore) hands back the very same object references it was given, so this
    // spec can NOT see a serialization regression. The wire format is guarded separately, by
    // NpcRolData_specs.Survives_a_json_round_trip and by Casting_survives_a_json_round_trip_of_the_event
    // below — do not delete those on the assumption that this one covers them.
    [Test]
    public async Task Casting_and_afstemming_survive_a_save_and_replay()
    {
        var eventStore = new InMemoryEventStore();
        var repository = new PlotboekPaginaRepository(eventStore);

        var pagina = new PlotboekPagina();
        pagina = pagina.AanPlotboekToevoegen(PlotboekId.Create(Year.Create(2025), EventIds.Voorjaar), "Voorjaar 2025", TestUser).Value;
        pagina = pagina.Aanpassen(
            new PlotboekPagina.Data
            {
                Titel = "Test",
                Dag = Dag.Vrijdag,
                StarttijdId = "Tijd1030",
                Order = 0,
                AantalNpcs = AantalNpcsData.Create([
                    NpcRolData.Create(
                        "slachtoffer",
                        1,
                        kledingEnSchmink: "gescheurde kleding",
                        castingInstructie: "goede roleplayer, stevig postuur",
                        vereistAfstemming: true,
                        afstemmingToelichting: "wordt gemarteld in scène 2"),
                ]),
            }, TestUser).Value;
        (await repository.SaveAsync(pagina, CancellationToken.None)).IsValid.Should().BeTrue();

        var replayed = await repository.LoadAsync(pagina.Id);

        replayed.IsValid.Should().BeTrue();
        var rol = replayed.Value.AantalNpcs.Rollen.Single();
        rol.CastingInstructie.Should().Be("goede roleplayer, stevig postuur");
        rol.VereistAfstemming.Should().BeTrue();
        rol.AfstemmingToelichting.Should().Be("wordt gemarteld in scène 2");
        rol.KledingEnSchmink.Should().Be("gescheurde kleding", "the pre-existing fields must keep working too");
        replayed.Value.AantalNpcs.HeeftAfstemmingNodig.Should().BeTrue();
    }

    // The other half: the event as Marten actually stores it. Marten 9 serializes with System.Text.Json
    // (no Marten.Newtonsoft package, no UseNewtonsoftForSerialization in Program.cs), so pushing the whole
    // event through STJ is a faithful stand-in for the wire format. The schema guard fingerprints only the
    // event's own properties, never the insides of NpcRolData, so this is the sole thing that would fail if
    // a casting field were renamed or dropped from the payload.
    [Test]
    public void Casting_survives_a_json_round_trip_of_the_event()
    {
        var pagina = CreatePagina();
        pagina = pagina.Aanpassen(
            new PlotboekPagina.Data
            {
                Titel = "Test",
                Dag = Dag.Vrijdag,
                StarttijdId = "Tijd1030",
                Order = 0,
                AantalNpcs = AantalNpcsData.Create([
                    NpcRolData.Create(
                        "slachtoffer",
                        1,
                        castingInstructie: "goede roleplayer",
                        vereistAfstemming: true,
                        afstemmingToelichting: "wordt gemarteld in scène 2"),
                ]),
            }, TestUser).Value;

        var appended = pagina.Buffer.Uncommitted.OfType<PlotboekPaginaAantalNpcsAangepastV2>().Single();
        var restored = JsonSerializer.Deserialize<PlotboekPaginaAantalNpcsAangepastV2>(JsonSerializer.Serialize(appended))!;

        var rol = restored.Rollen.Single();
        rol.CastingInstructie.Should().Be("goede roleplayer");
        rol.VereistAfstemming.Should().BeTrue();
        rol.AfstemmingToelichting.Should().Be("wordt gemarteld in scène 2");
    }

    // Only the casting text differs: AantalNpcsData.Equals runs SequenceEqual over the roles and NpcRolData
    // is a record, so structural equality picks the new fields up by itself and one event is appended.
    [Test]
    public void Aanpassen_only_casting_still_produces_an_event()
    {
        var pagina = CreatePagina();
        var data = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
            AantalNpcs = AantalNpcsData.Create([NpcRolData.Create("guard", 2)]),
        };

        pagina = pagina.Aanpassen(data, TestUser).Value;
        pagina.MarkAllAsCommitted();

        var result = pagina.Aanpassen(
            data with { AantalNpcs = AantalNpcsData.Create([NpcRolData.Create("guard", 2, castingInstructie: "stevig postuur")]) },
            TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.Buffer.Uncommitted.OfType<PlotboekPaginaAantalNpcsAangepastV2>().Should().ContainSingle();
        result.Value.AantalNpcs.Rollen.Single().CastingInstructie.Should().Be("stevig postuur");
    }

    [Test]
    public void Aanpassen_updates_plotlijn()
    {
        var pagina = CreatePagina();
        var plotlijnId = PlotlijnId.Next();
        var data = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
            PlotlijnId = plotlijnId,
        };

        var result = pagina.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.PlotlijnId.Should().Be(plotlijnId);
    }

    [Test]
    public void Aanpassen_updates_iconen()
    {
        var pagina = CreatePagina();
        var data = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
            Iconen = PlotboekPaginaIcons.TimingCruciaal | PlotboekPaginaIcons.PlotCruciaal,
        };

        var result = pagina.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.Iconen.Should().HaveFlag(PlotboekPaginaIcons.TimingCruciaal);
        result.Value.Iconen.Should().HaveFlag(PlotboekPaginaIcons.PlotCruciaal);
    }

    [Test]
    public void Aanpassen_updates_verantwoordelijke_sl()
    {
        var pagina = CreatePagina();
        var data = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
            VerantwoordelijkeSlId = "sl-1",
            VerantwoordelijkeSlName = "Alice",
            ExtraSlIds = ["sl-2"],
            ExtraSlNames = ["Bob"],
        };

        var result = pagina.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.VerantwoordelijkeSl.Should().Be("sl-1");
        result.Value.ExtraSlIds.Should().Contain("sl-2");
    }

    [Test]
    public void Aanpassen_iconen_noop_when_unchanged()
    {
        var pagina = CreatePagina();
        var data = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
            Iconen = PlotboekPaginaIcons.EventBreed,
        };

        pagina = pagina.Aanpassen(data, TestUser).Value;
        pagina.MarkAllAsCommitted();

        var result = pagina.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Aanpassen_plotlijn_noop_when_unchanged()
    {
        var pagina = CreatePagina();
        var plotlijnId = PlotlijnId.Next();
        var data = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
            PlotlijnId = plotlijnId,
        };

        pagina = pagina.Aanpassen(data, TestUser).Value;
        pagina.MarkAllAsCommitted();

        var result = pagina.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void GetData_returns_all_fields()
    {
        var pagina = CreatePagina();
        var plotlijnId = PlotlijnId.Next();
        var locatie = LocatieEncounterData.Create(LocatieEncounter.Hoofdveld, "Midden");
        var encounterTypeIds = EncounterTypeIds.Create([SoortEncounterIds.Vechten], [SoortEncounterIds.Roleplay]);
        var debrief = DebriefData.Create(true, ["Vraag 1"]);
        var propsEnLoot = PropsEnLootData.Create(
            [PropLootItemData.Create("Zwaard", "sl-1", "Alice")],
            [PropLootItemData.Create("Ring", "sl-2", "Bob")]);
        var aantalNpcs = new AantalNpcsData
        {
            AlleNpcs = false,
            Rollen = [NpcRolData.Create("guard", 3, 5)],
        };

        var data = new PlotboekPagina.Data
        {
            Titel = "Epic Battle",
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1430",
            Order = 2,
            DomainId = Guid.NewGuid(),
            DomainShorthand = "Ti",
            DomainDescription = "Titania",
            PlotlijnId = plotlijnId,
            AantalBlokkenId = "DrieBlokken",
            EncounterTypeIds = encounterTypeIds,
            LocatieEncounter = locatie,
            AantalNpcs = aantalNpcs,
            ElevatorPitch = "Een episch gevecht",
            SamenvattingPlot = "<p>Samenvatting</p>",
            MeerInformatie = "<p>Details</p>",
            InstructiePentagon = "Briefing",
            Iconen = PlotboekPaginaIcons.TimingCruciaal,
            PropsEnLoot = propsEnLoot,
            Opmerkingen = "Let op!",
            Debrief = debrief,
            IsVolledigKlaar = true,
            IsGedraaid = true,
            IsSpelerGedreven = true,
            IsFlexibel = true,
            IsFlexibelNachtPlot = true,
        };

        pagina = pagina.Aanpassen(data, TestUser).Value;

        var export = pagina.GetData();

        export.Titel.Should().Be("Epic Battle");
        export.Dag.Should().Be(Dag.Zaterdag);
        export.StarttijdId.Should().Be("Tijd1430");
        export.DomainShorthand.Should().Be("Ti");
        export.PlotlijnId.Should().Be(plotlijnId);
        export.AantalBlokkenId.Should().Be("DrieBlokken");
        export.EncounterTypeIds.Major.Should().BeEquivalentTo([SoortEncounterIds.Vechten]);
        export.EncounterTypeIds.Minor.Should().BeEquivalentTo([SoortEncounterIds.Roleplay]);
        export.LocatieEncounter.Locatie.Should().Be(LocatieEncounter.Hoofdveld);
        export.AantalNpcs.TotaalMinimum.Should().Be(3);
        export.ElevatorPitch.Should().Be("Een episch gevecht");
        export.SamenvattingPlot.Should().Be("<p>Samenvatting</p>");
        export.MeerInformatie.Should().Be("<p>Details</p>");
        export.InstructiePentagon.Should().Be("Briefing");
        export.Iconen.Should().HaveFlag(PlotboekPaginaIcons.TimingCruciaal);
        export.PropsEnLoot.Props.Should().ContainSingle();
        export.PropsEnLoot.Loot.Should().ContainSingle();
        export.Opmerkingen.Should().Be("Let op!");
        export.Debrief.IsDebriefGewenst.Should().BeTrue();
        export.IsVolledigKlaar.Should().BeTrue();
        export.IsGedraaid.Should().BeTrue();
        export.IsSpelerGedreven.Should().BeTrue();
        export.IsFlexibel.Should().BeTrue();
        export.IsFlexibelNachtPlot.Should().BeTrue();
    }
}
