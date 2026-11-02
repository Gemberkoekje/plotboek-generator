namespace PlotboekGenerator.test.Aggregates;

public sealed class PlotboekPagina_Aanpassen_specs
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
    public void AanPlotboekToevoegen_sets_plotboek_id()
    {
        var pagina = new PlotboekPagina();
        var plotboekId = PlotboekId.Create(Year.Create(2025), EventIds.Voorjaar);

        var result = pagina.AanPlotboekToevoegen(plotboekId, "Voorjaar 2025", TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.PlotboekId.Should().Be(plotboekId);
    }

    [Test]
    public void Aanpassen_updates_titel()
    {
        var pagina = CreatePagina();
        var data = new PlotboekPagina.Data
        {
            Titel = "De Handelaar",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
        };

        var result = pagina.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.Titel.Should().Be("De Handelaar");
    }

    [Test]
    public void Aanpassen_updates_dag()
    {
        var pagina = CreatePagina();
        var data = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1030",
            Order = 0,
        };

        var result = pagina.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.Dag.Should().Be(Dag.Zaterdag);
    }

    [Test]
    public void Aanpassen_updates_starttijd()
    {
        var pagina = CreatePagina();
        var data = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd2000",
            Order = 0,
        };

        var result = pagina.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.StarttijdId.Should().Be("Tijd2000");
    }

    [Test]
    public void Aanpassen_updates_elevator_pitch()
    {
        var pagina = CreatePagina();
        var data = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
            ElevatorPitch = "Een spannend avontuur.",
        };

        var result = pagina.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.ElevatorPitch.Should().Be("Een spannend avontuur.");
    }

    [Test]
    public void Aanpassen_updates_samenvatting_plot()
    {
        var pagina = CreatePagina();
        var data = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
            SamenvattingPlot = "<p>Uitgebreide samenvatting</p>",
        };

        var result = pagina.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.SamenvattingPlot.Should().Be("<p>Uitgebreide samenvatting</p>");
    }

    [Test]
    public void Aanpassen_updates_boolean_flags()
    {
        var pagina = CreatePagina();
        var data = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
            IsVolledigKlaar = true,
            IsGedraaid = true,
            IsSpelerGedreven = true,
            IsFlexibel = true,
        };

        var result = pagina.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.IsVolledigKlaar.Should().BeTrue();
        result.Value.IsGedraaid.Should().BeTrue();
        result.Value.IsSpelerGedreven.Should().BeTrue();
        result.Value.IsFlexibel.Should().BeTrue();
    }

    [Test]
    public void Aanpassen_updates_domain()
    {
        var pagina = CreatePagina();
        var domainId = Guid.NewGuid();
        var data = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
            DomainId = domainId,
            DomainShorthand = "Ti",
            DomainDescription = "Titania",
        };

        var result = pagina.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.DomainId.Should().Be(domainId);
        result.Value.DomainShorthand.Should().Be("Ti");
        result.Value.DomainDescription.Should().Be("Titania");
    }

    [Test]
    public void Aanpassen_noop_when_data_unchanged()
    {
        var pagina = CreatePagina();

        var data = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
        };

        pagina = pagina.Aanpassen(data, TestUser).Value;
        pagina.MarkAllAsCommitted();

        var result = pagina.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Aanpassen_throws_on_null_data()
    {
        var pagina = CreatePagina();

        var action = () => pagina.Aanpassen((PlotboekPagina.Data)null!, TestUser);

        action.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void SetVerwijderd_marks_as_deleted()
    {
        var pagina = CreatePagina();

        var result = pagina.SetVerwijderd(true, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.IsVerwijderd.Should().BeTrue();
    }

    [Test]
    public void SetVerwijderd_restores_when_set_to_false()
    {
        var pagina = CreatePagina();

        pagina = pagina.SetVerwijderd(true, TestUser).Value;
        pagina.MarkAllAsCommitted();

        var result = pagina.SetVerwijderd(false, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.IsVerwijderd.Should().BeFalse();
    }

    [Test]
    public void SetVerwijderd_noop_when_already_same_value()
    {
        var pagina = CreatePagina();

        var result = pagina.SetVerwijderd(false, TestUser);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void UpdateOrder_sets_order_dag_and_starttijd()
    {
        var pagina = CreatePagina();

        var result = pagina.UpdateOrder(3, Dag.Zaterdag, StarttijdIds.Tijd1430, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.Order.Should().Be(3);
        result.Value.Dag.Should().Be(Dag.Zaterdag);
        result.Value.StarttijdId.Should().Be(StarttijdIds.Tijd1430);
    }

    [Test]
    public void Aanpassen_updates_opmerkingen()
    {
        var pagina = CreatePagina();
        var data = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
            Opmerkingen = "Let op: extra NPC nodig",
        };

        var result = pagina.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.Opmerkingen.Should().Be("Let op: extra NPC nodig");
    }

    [Test]
    public void Aanpassen_updates_the_techniek_instruction()
    {
        var pagina = CreatePagina();
        var data = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
            InstructiePentagon = "<p>Techniek instructies</p>",
        };

        var result = pagina.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.InstructiePentagon.Should().Be("<p>Techniek instructies</p>");
    }

    [Test]
    public void Aanpassen_updates_meer_informatie()
    {
        var pagina = CreatePagina();
        var data = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
            MeerInformatie = "<p>Extra info</p>",
        };

        var result = pagina.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.MeerInformatie.Should().Be("<p>Extra info</p>");
    }

    [Test]
    public void Aanpassen_updates_encounter_types()
    {
        var pagina = CreatePagina();
        var encounterTypeIds = EncounterTypeIds.Create([SoortEncounterIds.Vechten], [SoortEncounterIds.Roleplay]);
        var data = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
            EncounterTypeIds = encounterTypeIds,
        };

        var result = pagina.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.EncounterTypeIds.Major.Should().BeEquivalentTo([SoortEncounterIds.Vechten]);
        result.Value.EncounterTypeIds.Minor.Should().BeEquivalentTo([SoortEncounterIds.Roleplay]);
    }

    [Test]
    public void Aanpassen_updates_props_en_loot()
    {
        var pagina = CreatePagina();
        var propsEnLoot = PropsEnLootData.Create(
            [PropLootItemData.Create("Zwaard", "sl-1", "Jan")],
            [PropLootItemData.Create("Goud", "sl-2", "Piet")]);
        var data = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
            PropsEnLoot = propsEnLoot,
        };

        var result = pagina.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.PropsEnLoot.Props.Should().ContainSingle();
        result.Value.PropsEnLoot.Loot.Should().ContainSingle();
    }

    [Test]
    public void Aanpassen_updates_debrief()
    {
        var pagina = CreatePagina();
        var debrief = DebriefData.Create(true, ["Hoe vond je het?"]);
        var data = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
            Debrief = debrief,
        };

        var result = pagina.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.Debrief.IsDebriefGewenst.Should().BeTrue();
    }
}
