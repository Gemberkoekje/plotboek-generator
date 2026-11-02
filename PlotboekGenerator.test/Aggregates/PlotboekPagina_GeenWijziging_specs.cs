namespace PlotboekGenerator.test.Aggregates;

public sealed class PlotboekPagina_GeenWijziging_specs
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
    public void Titel_noop_when_unchanged()
    {
        var pagina = CreatePagina();
        var data = new PlotboekPagina.Data
        {
            Titel = "De Handelaar",
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
    public void ElevatorPitch_noop_when_unchanged()
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

        pagina = pagina.Aanpassen(data, TestUser).Value;
        pagina.MarkAllAsCommitted();

        var result = pagina.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Domain_noop_when_unchanged()
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

        pagina = pagina.Aanpassen(data, TestUser).Value;
        pagina.MarkAllAsCommitted();

        var result = pagina.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void PropsEnLoot_noop_when_unchanged()
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

        pagina = pagina.Aanpassen(data, TestUser).Value;
        pagina.MarkAllAsCommitted();

        var result = pagina.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void SamenvattingPlot_noop_when_unchanged_and_line_endings_equivalent()
    {
        var pagina = CreatePagina();
        var stored = "<p>Line\nbreak</p>";
        var dataStore = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
            SamenvattingPlot = stored,
        };

        pagina = pagina.Aanpassen(dataStore, TestUser).Value;
        pagina.MarkAllAsCommitted();

        // incoming with CRLF line endings should be normalized and therefore not cause an event
        var incoming = stored.Replace("\n", "\r\n");
        var dataIncoming = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
            SamenvattingPlot = incoming,
        };

        var result = pagina.Aanpassen(dataIncoming, TestUser);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void MeerInformatie_noop_when_unchanged_and_line_endings_equivalent()
    {
        var pagina = CreatePagina();
        var stored = "<p>Info\nregels</p>";
        var dataStore = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
            MeerInformatie = stored,
        };

        pagina = pagina.Aanpassen(dataStore, TestUser).Value;
        pagina.MarkAllAsCommitted();

        var incoming = stored.Replace("\n", "\r\n");
        var dataIncoming = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
            MeerInformatie = incoming,
        };

        var result = pagina.Aanpassen(dataIncoming, TestUser);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void InstructiePentagon_noop_when_unchanged_and_line_endings_equivalent()
    {
        var pagina = CreatePagina();
        var stored = "<p>Techniek\ninstructie</p>";
        var dataStore = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
            InstructiePentagon = stored,
        };

        pagina = pagina.Aanpassen(dataStore, TestUser).Value;
        pagina.MarkAllAsCommitted();

        var incoming = stored.Replace("\n", "\r\n");
        var dataIncoming = new PlotboekPagina.Data
        {
            Titel = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 0,
            InstructiePentagon = incoming,
        };

        var result = pagina.Aanpassen(dataIncoming, TestUser);

        result.IsValid.Should().BeTrue();
    }
}
