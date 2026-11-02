namespace PlotboekGenerator.test.Services;

public sealed class PlotboekExportBuilder_BuildRecords_specs
{
    [Test]
    public async Task BuildRecordsAsync_uses_config_display_names_and_ordering()
    {
        var store = new TestDocumentStore();
        var sut = new PlotboekExportBuilder(store);

        var jaar = Year.Create(2026);
        var plotboekId = PlotboekId.Create(jaar, EventIds.Voorjaar);

        store.Seed(new Plotboek.ExportData
        {
            PlotboekId = plotboekId,
            Jaar = jaar,
            EventId = EventIds.Voorjaar,
            StartDag = Dag.Vrijdag,
            EindDag = Dag.Zaterdag,
            Titel = "Test Plotboek",
        });

        store.Seed(new EventConfig
        {
            Id = EventIds.Voorjaar,
            EnumValue = 1,
            DisplayName = "Voorjaarsevent Custom",
            Abbreviation = "VJ",
            Order = 1,
            IsEvergreen = false,
            IsNone = false,
        });

        store.Seed(
            new StarttijdConfig
            {
                Id = StarttijdIds.None,
                EnumValue = 0,
                DisplayName = "-",
                IsAvailableOnEersteDag = false,
                IsAvailableOnMiddelDagen = false,
                IsAvailableOnLaatsteDag = false,
                Order = 0,
                IsNone = true,
            },
            new StarttijdConfig
            {
                Id = StarttijdIds.Tijd1430,
                EnumValue = 3,
                DisplayName = "14:30 custom",
                IsAvailableOnEersteDag = false,
                IsAvailableOnMiddelDagen = true,
                IsAvailableOnLaatsteDag = true,
                Order = 1,
                IsNone = false,
            },
            new StarttijdConfig
            {
                Id = StarttijdIds.Tijd1030,
                EnumValue = 1,
                DisplayName = "10:30 custom",
                IsAvailableOnEersteDag = false,
                IsAvailableOnMiddelDagen = true,
                IsAvailableOnLaatsteDag = true,
                Order = 2,
                IsNone = false,
            });

        store.Seed(new AantalBlokkenConfig
        {
            Id = AantalBlokkenIds.EenBlok,
            EnumValue = 1,
            DisplayName = "One block custom",
            OverflowBlokken = 0,
            Order = 1,
            IsNone = false,
        });

        store.Seed(new SoortEncounterConfig
        {
            Id = SoortEncounterIds.Roleplay,
            EnumValue = 2,
            DisplayName = "Roleplay custom",
            Order = 1,
            IsNone = false,
        });

        store.Seed(
            CreatePage(plotboekId, "Later by id but first by config order", StarttijdIds.Tijd1430, order: 1),
            CreatePage(plotboekId, "Earlier by id but second by config order", StarttijdIds.Tijd1030, order: 1));

        using var session = store.OpenSession();
        var result = await sut.BuildRecordsAsync(plotboekId, session, CancellationToken.None);

        result.Naam.Should().Contain("Voorjaarsevent Custom");
        result.Days.Should().HaveCount(2);
        result.Days[0].Day.Should().Contain("14:30 custom");
        result.Days[0].Pages.Should().ContainSingle();
        result.Days[0].Pages[0].NaamEncounter.Should().Contain("Later by id but first by config order");
        result.Days[0].Pages[0].GeschatteTijdsduur.Should().Be("One block custom");
        result.Days[0].Pages[0].SoortEncounter.Should().Be("Roleplay custom");
    }

    [Test]
    public async Task BuildRecordsAsync_falls_back_to_raw_ids_when_config_is_missing()
    {
        var store = new TestDocumentStore();
        var sut = new PlotboekExportBuilder(store);

        var jaar = Year.Create(2027);
        var plotboekId = PlotboekId.Create(jaar, EventIds.Najaar);

        store.Seed(new Plotboek.ExportData
        {
            PlotboekId = plotboekId,
            Jaar = jaar,
            EventId = "UnknownEvent",
            StartDag = Dag.Vrijdag,
            EindDag = Dag.Vrijdag,
            Titel = "",
        });

        store.Seed(CreatePage(plotboekId, "Fallback test", "UnknownStarttijd", order: 1, aantalBlokkenId: "UnknownBlokken", encounterTypeId: "UnknownEncounter"));

        using var session = store.OpenSession();
        var result = await sut.BuildRecordsAsync(plotboekId, session, CancellationToken.None);

        result.Naam.Should().Contain("UnknownEvent");
        result.Days.Should().ContainSingle();
        result.Days[0].Day.Should().Contain("UnknownStarttijd");
        result.Days[0].Pages.Should().ContainSingle();
        result.Days[0].Pages[0].GeschatteTijdsduur.Should().Be("UnknownBlokken");
        result.Days[0].Pages[0].SoortEncounter.Should().Be("UnknownEncounter");
    }

    [Test]
    public async Task BuildRecordsAsync_resolves_a_picked_questlocatie_to_its_display_name()
    {
        var store = new TestDocumentStore();
        var sut = new PlotboekExportBuilder(store);

        var jaar = Year.Create(2028);
        var plotboekId = PlotboekId.Create(jaar, EventIds.Voorjaar);

        store.Seed(new Plotboek.ExportData
        {
            PlotboekId = plotboekId,
            Jaar = jaar,
            EventId = EventIds.Voorjaar,
            StartDag = Dag.Vrijdag,
            EindDag = Dag.Vrijdag,
            Titel = "Test Plotboek",
        });

        store.Seed(new QuestLocatieConfig
        {
            Id = "Ruine",
            DisplayName = "Ruïne",
            CategorieId = "Plekken",
            IsActief = true,
            Order = 1,
        });

        store.Seed(
            CreatePage(plotboekId, "Met keuze", StarttijdIds.Tijd1030, order: 1) with
            {
                LocatieEncounter = LocatieEncounterData.Create(LocatieEncounter.Questveld, locatieId: "Ruine"),
            },
            CreatePage(plotboekId, "Verwijderde locatie", StarttijdIds.Tijd1030, order: 2) with
            {
                LocatieEncounter = LocatieEncounterData.Create(LocatieEncounter.Questveld, locatieId: "BestaatNietMeer"),
            },
            CreatePage(plotboekId, "Vrije tekst", StarttijdIds.Tijd1030, order: 3) with
            {
                LocatieEncounter = LocatieEncounterData.Create(LocatieEncounter.Questveld, "Bij de grote eik"),
            });

        using var session = store.OpenSession();
        var result = await sut.BuildRecordsAsync(plotboekId, session, CancellationToken.None);

        var pages = result.Days.SelectMany(d => d.Pages).ToList();
        pages.Should().HaveCount(3);
        pages[0].LocatieEncounter.Should().Be("Questveld (Ruïne)", "a rename must flow through to the export");
        pages[1].LocatieEncounter.Should().Be("Questveld (BestaatNietMeer)", "the raw id beats losing the location entirely");
        pages[2].LocatieEncounter.Should().Be("Questveld (Bij de grote eik)");
    }

    private static PlotboekPagina.ExportData CreatePage(
        PlotboekId plotboekId,
        string title,
        string starttijdId,
        int order,
        string aantalBlokkenId = AantalBlokkenIds.EenBlok,
        string encounterTypeId = SoortEncounterIds.Roleplay)
    {
        return new PlotboekPagina.ExportData
        {
            PlotboekId = plotboekId,
            PaginaId = PlotboekPaginaId.Next(),
            Titel = title,
            Dag = Dag.Vrijdag,
            StarttijdId = starttijdId,
            Order = order,
            VerantwoordelijkeSl = string.Empty,
            AantalBlokkenId = aantalBlokkenId,
            EncounterTypeIds = EncounterTypeIds.Create([encounterTypeId], []),

            DomainShorthand = string.Empty,
            DomainDescription = string.Empty,
            LocatieEncounter = LocatieEncounterData.Empty,
            AantalNpcs = AantalNpcsData.Empty,
            ElevatorPitch = string.Empty,
            SamenvattingPlot = string.Empty,
            MeerInformatie = string.Empty,
            InstructiePentagon = string.Empty,
            Opmerkingen = string.Empty,
            PropsEnLoot = PropsEnLootData.Empty,
            Debrief = DebriefData.Empty,
            Iconen = PlotboekPaginaIcons.None,
        };
    }
}
