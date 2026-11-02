namespace PlotboekGenerator.test.Pages;

public sealed class PlotboekModel_BerekenLocatieConflicten_specs
{
    private static PlotboekModel CreateModel()
        => new (null!, null!, null!, null!, null!, null!, null!, null!, null!)
        {
            AvailableStarttijdConfigs =
            [
                new StarttijdConfig { Id = StarttijdIds.None, EnumValue = 0, DisplayName = "-", IsNone = true, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = false, IsAvailableOnLaatsteDag = false, Order = 0 },
                new StarttijdConfig { Id = StarttijdIds.Tijd1030, EnumValue = 1, DisplayName = "10:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = true, Order = 1 },
                new StarttijdConfig { Id = StarttijdIds.Tijd1230, EnumValue = 2, DisplayName = "12:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = true, Order = 2 },
                new StarttijdConfig { Id = StarttijdIds.Tijd1430, EnumValue = 3, DisplayName = "14:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = true, Order = 3 },
            ],
            AvailableAantalBlokkenConfigs =
            [
                new AantalBlokkenConfig { Id = AantalBlokkenIds.None, EnumValue = 0, DisplayName = "-", IsNone = true, OverflowBlokken = 0, Order = 0 },
                new AantalBlokkenConfig { Id = AantalBlokkenIds.EenBlok, EnumValue = 1, DisplayName = "1 blok", IsNone = false, OverflowBlokken = 0, Order = 1 },
                new AantalBlokkenConfig { Id = AantalBlokkenIds.TweeBlokken, EnumValue = 2, DisplayName = "2 blokken", IsNone = false, OverflowBlokken = 1, Order = 2 },
            ],
            AvailableQuestLocatieConfigs =
            [
                new QuestLocatieConfig { Id = "Leeuwenkuil", DisplayName = "Leeuwenkuil", CategorieId = "Plekken", IsActief = true, Order = 1 },
                new QuestLocatieConfig { Id = "Ruine", DisplayName = "Ruïne", CategorieId = "Plekken", IsActief = true, Order = 2 },

                // Only one side of the overlap is filled in; the reader makes it symmetric.
                new QuestLocatieConfig { Id = "RouteLeeuwenkuil", DisplayName = "Route naar Leeuwenkuil", CategorieId = "Routes", ConflicteertMet = ["Leeuwenkuil"], IsActief = true, Order = 1 },
            ],
        };

    private static PlotboekModel.PlotboekPaginaInfo Pagina(
        string titel,
        string locatieId,
        Dag dag = Dag.Vrijdag,
        string starttijdId = StarttijdIds.Tijd1030,
        string aantalBlokken = AantalBlokkenIds.EenBlok,
        bool isSpelerGedreven = false)
        => new ()
        {
            Id = PlotboekPaginaId.Next(),
            Title = titel,
            Dag = dag,
            StarttijdId = starttijdId,
            Order = 1,
            AantalBlokkenId = aantalBlokken,
            IsSpelerGedreven = isSpelerGedreven,
            LocatieEncounter = locatieId is null
                ? LocatieEncounterData.Empty
                : LocatieEncounterData.Create(LocatieEncounter.Questveld, locatieId: locatieId),
        };

    [Test]
    public void Same_locatie_in_the_same_slot_is_an_exact_clash_for_both_plots()
    {
        var model = CreateModel();
        var a = Pagina("Plot A", "Leeuwenkuil");
        var b = Pagina("Plot B", "Leeuwenkuil");
        model.PlotboekPaginas = [a, b];

        model.BerekenLocatieConflicten();

        model.GetLocatieConflicten(a.Id).Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Titel = "Plot B", Soort = LocatieConflictSoort.ExacteMatch });
        model.GetLocatieConflicten(b.Id).Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Titel = "Plot A", Soort = LocatieConflictSoort.ExacteMatch });
    }

    [Test]
    public void Same_locatie_in_a_different_slot_does_not_clash()
    {
        var model = CreateModel();
        model.PlotboekPaginas =
        [
            Pagina("Plot A", "Leeuwenkuil", starttijdId: StarttijdIds.Tijd1030),
            Pagina("Plot B", "Leeuwenkuil", starttijdId: StarttijdIds.Tijd1230),
        ];

        model.BerekenLocatieConflicten();

        model.LocatieConflicten.Should().BeEmpty();
    }

    [Test]
    public void Same_locatie_on_a_different_day_does_not_clash()
    {
        var model = CreateModel();
        model.PlotboekPaginas =
        [
            Pagina("Plot A", "Leeuwenkuil", dag: Dag.Vrijdag),
            Pagina("Plot B", "Leeuwenkuil", dag: Dag.Zaterdag),
        ];

        model.BerekenLocatieConflicten();

        model.LocatieConflicten.Should().BeEmpty();
    }

    [Test]
    public void A_multi_block_plot_clashes_with_the_slot_it_runs_into()
    {
        var model = CreateModel();
        var a = Pagina("Plot A", "Leeuwenkuil", starttijdId: StarttijdIds.Tijd1030, aantalBlokken: AantalBlokkenIds.TweeBlokken);
        var b = Pagina("Plot B", "Leeuwenkuil", starttijdId: StarttijdIds.Tijd1230);
        model.PlotboekPaginas = [a, b];

        model.BerekenLocatieConflicten();

        model.GetLocatieConflicten(a.Id).Should().ContainSingle()
            .Which.Soort.Should().Be(LocatieConflictSoort.ExacteMatch, "the two-block plot still occupies 12:30");
        model.GetLocatieConflicten(b.Id).Should().ContainSingle();
    }

    [Test]
    public void Physically_overlapping_locaties_produce_a_soft_warning()
    {
        var model = CreateModel();
        var a = Pagina("Plot A", "Leeuwenkuil");
        var b = Pagina("Plot B", "RouteLeeuwenkuil");
        model.PlotboekPaginas = [a, b];

        model.BerekenLocatieConflicten();

        model.GetLocatieConflicten(a.Id).Should().ContainSingle()
            .Which.Soort.Should().Be(LocatieConflictSoort.Overlapgroep, "the overlap is made symmetric when read");
        model.GetLocatieConflicten(b.Id).Should().ContainSingle()
            .Which.Soort.Should().Be(LocatieConflictSoort.Overlapgroep);
    }

    [Test]
    public void Different_unrelated_locaties_do_not_clash()
    {
        var model = CreateModel();
        model.PlotboekPaginas = [Pagina("Plot A", "Leeuwenkuil"), Pagina("Plot B", "Ruine")];

        model.BerekenLocatieConflicten();

        model.LocatieConflicten.Should().BeEmpty();
    }

    [TestCase(Dag.None, StarttijdIds.Tijd1030, false, TestName = "no day")]
    [TestCase(Dag.Vrijdag, StarttijdIds.None, false, TestName = "no starttijd")]
    [TestCase(Dag.Vrijdag, StarttijdIds.Tijd1030, true, TestName = "speler gedreven")]
    public void A_plot_without_a_time_slot_only_gets_the_soft_note(Dag dag, string starttijdId, bool isSpelerGedreven)
    {
        var model = CreateModel();
        var a = Pagina("Plot A", "Leeuwenkuil");
        var b = Pagina("Plot B", "Leeuwenkuil", dag, starttijdId, isSpelerGedreven: isSpelerGedreven);
        model.PlotboekPaginas = [a, b];

        model.BerekenLocatieConflicten();

        model.GetLocatieConflicten(a.Id).Should().ContainSingle()
            .Which.Soort.Should().Be(LocatieConflictSoort.ZonderTijdvak);
    }

    [Test]
    public void An_overlapping_locatie_without_a_time_slot_is_not_worth_a_note()
    {
        var model = CreateModel();
        model.PlotboekPaginas =
        [
            Pagina("Plot A", "Leeuwenkuil"),
            Pagina("Plot B", "RouteLeeuwenkuil", dag: Dag.None),
        ];

        model.BerekenLocatieConflicten();

        model.LocatieConflicten.Should().BeEmpty("an adjacent field is only interesting when the times collide");
    }

    [Test]
    public void Free_text_locaties_never_take_part()
    {
        var model = CreateModel();
        var a = new PlotboekModel.PlotboekPaginaInfo
        {
            Id = PlotboekPaginaId.Next(),
            Title = "Plot A",
            Dag = Dag.Vrijdag,
            StarttijdId = StarttijdIds.Tijd1030,
            Order = 1,
            AantalBlokkenId = AantalBlokkenIds.EenBlok,
            LocatieEncounter = LocatieEncounterData.Create(LocatieEncounter.Questveld, "Bij de grote eik"),
        };
        var b = a with { Id = PlotboekPaginaId.Next(), Title = "Plot B" };
        model.PlotboekPaginas = [a, b];

        model.BerekenLocatieConflicten();

        model.LocatieConflicten.Should().BeEmpty("typo-prone free text is too unreliable to compare");
    }

    [Test]
    public void Conflicts_are_projected_onto_the_pagina_infos_for_the_list_icon()
    {
        var model = CreateModel();
        model.PlotboekPaginas = [Pagina("Plot A", "Leeuwenkuil"), Pagina("Plot B", "Leeuwenkuil")];

        model.BerekenLocatieConflicten();

        model.PlotboekPaginas.Should().OnlyContain(p => p.LocatieConflicten.Count == 1);
    }

    [Test]
    public void A_recalculation_clears_the_previous_result()
    {
        var model = CreateModel();
        model.PlotboekPaginas = [Pagina("Plot A", "Leeuwenkuil"), Pagina("Plot B", "Leeuwenkuil")];
        model.BerekenLocatieConflicten();

        model.PlotboekPaginas = [Pagina("Plot A", "Leeuwenkuil"), Pagina("Plot B", "Ruine")];
        model.BerekenLocatieConflicten();

        model.LocatieConflicten.Should().BeEmpty();
        model.PlotboekPaginas.Should().OnlyContain(p => p.LocatieConflicten.Count == 0);
    }

    [Test]
    public void Exact_clashes_are_listed_before_softer_ones()
    {
        var model = CreateModel();
        var a = Pagina("Plot A", "Leeuwenkuil");
        model.PlotboekPaginas =
        [
            a,
            Pagina("Zachte botsing", "RouteLeeuwenkuil"),
            Pagina("Harde botsing", "Leeuwenkuil"),
        ];

        model.BerekenLocatieConflicten();

        model.GetLocatieConflicten(a.Id).Select(c => c.Soort).Should().BeEquivalentTo(
            [LocatieConflictSoort.ExacteMatch, LocatieConflictSoort.Overlapgroep],
            o => o.WithStrictOrdering());
        model.HeeftExacteLocatieBotsing(a.Id).Should().BeTrue();
    }

    [Test]
    public void A_slot_that_only_exists_on_the_last_day_does_not_shorten_a_middle_day_span()
    {
        var model = CreateModel();
        model.AvailableDays = [Dag.Vrijdag, Dag.Zaterdag, Dag.Zondag];
        model.AvailableStarttijdConfigs =
        [
            .. model.AvailableStarttijdConfigs,
            new StarttijdConfig { Id = StarttijdIds.Tijd1130, EnumValue = 9, DisplayName = "11:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = false, IsAvailableOnLaatsteDag = true, Order = 2, BlokVensterLaatsteDag = "11:30 - 13:30" },
        ];

        var a = Pagina("Plot A", "Leeuwenkuil", dag: Dag.Zaterdag, starttijdId: StarttijdIds.Tijd1030, aantalBlokken: AantalBlokkenIds.TweeBlokken);
        var b = Pagina("Plot B", "Leeuwenkuil", dag: Dag.Zaterdag, starttijdId: StarttijdIds.Tijd1230);
        model.PlotboekPaginas = [a, b];

        model.BerekenLocatieConflicten();

        model.GetLocatieConflicten(a.Id).Should().ContainSingle(
            because: "Saturday has no 11:30, so the two-block plot still runs into 12:30 and clashes there")
            .Which.Should().BeEquivalentTo(new { Titel = "Plot B", Soort = LocatieConflictSoort.ExacteMatch });
    }
}
