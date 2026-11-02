using PlotboekGenerator.core.Events;

namespace PlotboekGenerator.test.Aggregates;

/// <summary>
/// Guards the single-field plotlijn setter used when linking two paginas together. Linking must only
/// change the plotlijn association: a regression where a full <see cref="PlotboekPagina.Aanpassen"/>
/// was used instead wiped the linked pagina's entire content (elevator pitch, plot summary, NPCs, ...).
/// </summary>
public sealed class PlotboekPagina_AanpassenPlotlijn_specs
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

    /// <summary>
    /// Builds a fully populated pagina so we can assert that nothing but the plotlijn changes.
    /// </summary>
    private static PlotboekPagina CreateFullyPopulatedPagina()
    {
        var pagina = new PlotboekPagina();
        var plotboekId = PlotboekId.Create(Year.Create(2025), EventIds.Voorjaar);
        pagina = pagina.AanPlotboekToevoegen(plotboekId, "Voorjaar 2025", TestUser).Value;

        var data = new PlotboekPagina.Data
        {
            Titel = "Volledige plot",
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1430",
            Order = 2,
            DomainId = Guid.NewGuid(),
            DomainShorthand = "Ti",
            DomainDescription = "Titania",
            AantalBlokkenId = "DrieBlokken",
            EncounterTypeIds = EncounterTypeIds.Create([SoortEncounterIds.Vechten], [SoortEncounterIds.Roleplay]),
            LocatieEncounter = LocatieEncounterData.Create(LocatieEncounter.Hoofdveld, "Midden"),
            AantalNpcs = new AantalNpcsData { AlleNpcs = false, Rollen = [NpcRolData.Create("guard", 3, 5)] },
            ElevatorPitch = "Een episch gevecht",
            SamenvattingPlot = "<p>Samenvatting</p>",
            MeerInformatie = "<p>Details</p>",
            InstructiePentagon = "Briefing",
            Iconen = PlotboekPaginaIcons.TimingCruciaal | PlotboekPaginaIcons.PlotCruciaal,
            PropsEnLoot = PropsEnLootData.Create(
                [PropLootItemData.Create("Zwaard", "sl-1", "Alice")],
                [PropLootItemData.Create("Ring", "sl-2", "Bob")]),
            Opmerkingen = "Let op!",
            Debrief = DebriefData.Create(true, ["Vraag 1"]),
            VerantwoordelijkeSlId = "sl-1",
            VerantwoordelijkeSlName = "Alice",
            ExtraSlIds = ["sl-2"],
            ExtraSlNames = ["Bob"],
            IsVolledigKlaar = true,
            IsGedraaid = true,
            IsSpelerGedreven = true,
            IsFlexibel = true,
            IsFlexibelNachtPlot = true,
        };

        pagina = pagina.Aanpassen(data, TestUser).Value;
        pagina.MarkAllAsCommitted();
        return pagina;
    }

    [Test]
    public void AanpassenPlotlijn_sets_the_plotlijn_id()
    {
        var pagina = CreateFullyPopulatedPagina();
        var plotlijnId = PlotlijnId.Next();

        var result = pagina.AanpassenPlotlijn(plotlijnId, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.PlotlijnId.Should().Be(plotlijnId);
    }

    [Test]
    public void AanpassenPlotlijn_preserves_all_other_content()
    {
        var pagina = CreateFullyPopulatedPagina();
        var before = pagina.GetData();
        var plotlijnId = PlotlijnId.Next();

        var after = pagina.AanpassenPlotlijn(plotlijnId, TestUser).Value.GetData();

        // Only the plotlijn changes; everything else is exactly as it was.
        after.PlotlijnId.Should().Be(plotlijnId);
        after.Titel.Should().Be(before.Titel);
        after.DomainId.Should().Be(before.DomainId);
        after.DomainShorthand.Should().Be(before.DomainShorthand);
        after.DomainDescription.Should().Be(before.DomainDescription);
        after.Dag.Should().Be(before.Dag);
        after.StarttijdId.Should().Be(before.StarttijdId);
        after.Order.Should().Be(before.Order);
        after.VerantwoordelijkeSl.Should().Be(before.VerantwoordelijkeSl);
        after.ExtraSlIds.Should().BeEquivalentTo(before.ExtraSlIds);
        after.AantalBlokkenId.Should().Be(before.AantalBlokkenId);
        after.EncounterTypeIds.Major.Should().BeEquivalentTo(before.EncounterTypeIds.Major);
        after.EncounterTypeIds.Minor.Should().BeEquivalentTo(before.EncounterTypeIds.Minor);
        after.LocatieEncounter.Locatie.Should().Be(before.LocatieEncounter.Locatie);
        after.LocatieEncounter.Specificatie.Should().Be(before.LocatieEncounter.Specificatie);
        after.AantalNpcs.TotaalMinimum.Should().Be(before.AantalNpcs.TotaalMinimum);
        after.ElevatorPitch.Should().Be(before.ElevatorPitch);
        after.SamenvattingPlot.Should().Be(before.SamenvattingPlot);
        after.MeerInformatie.Should().Be(before.MeerInformatie);
        after.InstructiePentagon.Should().Be(before.InstructiePentagon);
        after.Iconen.Should().Be(before.Iconen);
        after.PropsEnLoot.Props.Should().HaveCount(before.PropsEnLoot.Props.Count);
        after.PropsEnLoot.Loot.Should().HaveCount(before.PropsEnLoot.Loot.Count);
        after.Opmerkingen.Should().Be(before.Opmerkingen);
        after.Debrief.IsDebriefGewenst.Should().Be(before.Debrief.IsDebriefGewenst);
        after.IsVolledigKlaar.Should().Be(before.IsVolledigKlaar);
        after.IsGedraaid.Should().Be(before.IsGedraaid);
        after.IsSpelerGedreven.Should().Be(before.IsSpelerGedreven);
        after.IsFlexibel.Should().Be(before.IsFlexibel);
        after.IsFlexibelNachtPlot.Should().Be(before.IsFlexibelNachtPlot);
    }

    [Test]
    public void AanpassenPlotlijn_emits_a_single_plotlijn_event()
    {
        var pagina = CreateFullyPopulatedPagina();

        var result = pagina.AanpassenPlotlijn(PlotlijnId.Next(), TestUser);

        result.IsValid.Should().BeTrue();
        // A content-preserving link must not append content-mutating events.
        result.Value.Buffer.Uncommitted.Should().ContainSingle()
            .Which.Should().BeOfType<PlotboekPaginaPlotlijnAangepast>();
    }

    [Test]
    public void AanpassenPlotlijn_is_noop_when_unchanged()
    {
        var pagina = CreateFullyPopulatedPagina();
        var plotlijnId = PlotlijnId.Next();
        pagina = pagina.AanpassenPlotlijn(plotlijnId, TestUser).Value;
        pagina.MarkAllAsCommitted();

        var result = pagina.AanpassenPlotlijn(plotlijnId, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.Buffer.Uncommitted.Should().BeEmpty();
    }
}
