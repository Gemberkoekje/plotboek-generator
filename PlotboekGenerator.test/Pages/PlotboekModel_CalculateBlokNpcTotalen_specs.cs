namespace PlotboekGenerator.test.Pages;

public sealed class PlotboekModel_CalculateBlokNpcTotalen_specs
{
    private static PlotboekModel CreateModel()
    {
        var model = new PlotboekModel(null!, null!, null!, null!, null!, null!, null!, null!, null!);
        model.AvailableStarttijdConfigs =
        [
            new StarttijdConfig { Id = StarttijdIds.None, EnumValue = 0, DisplayName = "-", IsNone = true, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = false, IsAvailableOnLaatsteDag = false, Order = 0 },
            new StarttijdConfig { Id = StarttijdIds.Tijd1030, EnumValue = 1, DisplayName = "10:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = true, Order = 1 },
            new StarttijdConfig { Id = StarttijdIds.Tijd1230, EnumValue = 2, DisplayName = "12:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = true, Order = 2 },
            new StarttijdConfig { Id = StarttijdIds.Tijd1430, EnumValue = 3, DisplayName = "14:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = true, Order = 3 },
            new StarttijdConfig { Id = StarttijdIds.Tijd1630, EnumValue = 4, DisplayName = "16:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = false, Order = 4 },
            new StarttijdConfig { Id = StarttijdIds.Tijd1930Introquest, EnumValue = 5, DisplayName = "19:30: Introquest", IsNone = false, IsAvailableOnEersteDag = true, IsAvailableOnMiddelDagen = false, IsAvailableOnLaatsteDag = false, Order = 5 },
            new StarttijdConfig { Id = StarttijdIds.Tijd2000, EnumValue = 6, DisplayName = "20:00", IsNone = false, IsAvailableOnEersteDag = true, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = false, Order = 6 },
            new StarttijdConfig { Id = StarttijdIds.Tijd2200, EnumValue = 7, DisplayName = "22:00", IsNone = false, IsAvailableOnEersteDag = true, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = false, Order = 7 },
            new StarttijdConfig { Id = StarttijdIds.Tijd0000, EnumValue = 8, DisplayName = "00:00", IsNone = false, IsAvailableOnEersteDag = true, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = false, Order = 8 },
        ];

        model.AvailableAantalBlokkenConfigs =
        [
            new AantalBlokkenConfig { Id = AantalBlokkenIds.None, EnumValue = 0, DisplayName = "-", IsNone = true, OverflowBlokken = 0, Order = 0 },
            new AantalBlokkenConfig { Id = AantalBlokkenIds.EenBlok, EnumValue = 1, DisplayName = "1 blok", IsNone = false, OverflowBlokken = 0, Order = 1 },
            new AantalBlokkenConfig { Id = AantalBlokkenIds.TweeBlokken, EnumValue = 2, DisplayName = "2 blokken", IsNone = false, OverflowBlokken = 1, Order = 2 },
            new AantalBlokkenConfig { Id = AantalBlokkenIds.DrieBlokken, EnumValue = 3, DisplayName = "3 blokken", IsNone = false, OverflowBlokken = 2, Order = 3 },
        ];

        return model;
    }

    [Test]
    public void Empty_paginas_produces_empty_totals()
    {
        var model = CreateModel();
        model.PlotboekPaginas = [];

        model.CalculateBlokNpcTotalen();

        model.BlokNpcTotalen.Should().BeEmpty();
    }

    [Test]
    public void Ignores_paginas_with_no_dag()
    {
        var model = CreateModel();
        model.PlotboekPaginas =
        [
            CreatePagina(Dag.None, StarttijdIds.Tijd1030, npcsMin: 3),
        ];

        model.CalculateBlokNpcTotalen();

        model.BlokNpcTotalen.Should().BeEmpty();
    }

    [Test]
    public void Ignores_paginas_with_no_starttijd()
    {
        var model = CreateModel();
        model.PlotboekPaginas =
        [
            CreatePagina(Dag.Vrijdag, StarttijdIds.None, npcsMin: 3),
        ];

        model.CalculateBlokNpcTotalen();

        model.BlokNpcTotalen.Should().BeEmpty();
    }

    [Test]
    public void Ignores_speler_gedreven_paginas()
    {
        var model = CreateModel();
        model.PlotboekPaginas =
        [
            CreatePagina(Dag.Vrijdag, StarttijdIds.Tijd1030, npcsMin: 3, isSpelerGedreven: true),
        ];

        model.CalculateBlokNpcTotalen();

        model.BlokNpcTotalen.Should().BeEmpty();
    }

    [Test]
    public void Sums_direct_NPC_minimums_in_same_block()
    {
        var model = CreateModel();
        model.PlotboekPaginas =
        [
            CreatePagina(Dag.Vrijdag, StarttijdIds.Tijd1030, npcsMin: 3),
            CreatePagina(Dag.Vrijdag, StarttijdIds.Tijd1030, npcsMin: 5),
        ];

        model.CalculateBlokNpcTotalen();

        var totaal = model.BlokNpcTotalen[(Dag.Vrijdag, StarttijdIds.Tijd1030)];
        totaal.DirectMinimum.Should().Be(8);
    }

    [Test]
    public void Sums_direct_NPC_maximums_in_same_block()
    {
        var model = CreateModel();
        model.PlotboekPaginas =
        [
            CreatePagina(Dag.Vrijdag, StarttijdIds.Tijd1030, npcsMin: 2, npcsMax: 4),
            CreatePagina(Dag.Vrijdag, StarttijdIds.Tijd1030, npcsMin: 3, npcsMax: 6),
        ];

        model.CalculateBlokNpcTotalen();

        var totaal = model.BlokNpcTotalen[(Dag.Vrijdag, StarttijdIds.Tijd1030)];
        totaal.DirectMaximum.Should().Be(10);
    }

    [Test]
    public void DirectMaximum_is_null_when_any_pagina_has_AlleNpcs()
    {
        var model = CreateModel();
        model.PlotboekPaginas =
        [
            CreatePagina(Dag.Vrijdag, StarttijdIds.Tijd1030, npcsMin: 2, npcsMax: 4),
            CreatePagina(Dag.Vrijdag, StarttijdIds.Tijd1030, alleNpcs: true),
        ];

        model.CalculateBlokNpcTotalen();

        var totaal = model.BlokNpcTotalen[(Dag.Vrijdag, StarttijdIds.Tijd1030)];
        totaal.DirectMaximum.Should().BeNull();
        totaal.HeeftAlleNpcs.Should().BeTrue();
    }

    [Test]
    public void Separate_days_produce_separate_totals()
    {
        var model = CreateModel();
        model.PlotboekPaginas =
        [
            CreatePagina(Dag.Vrijdag, StarttijdIds.Tijd1030, npcsMin: 3),
            CreatePagina(Dag.Zaterdag, StarttijdIds.Tijd1030, npcsMin: 5),
        ];

        model.CalculateBlokNpcTotalen();

        model.BlokNpcTotalen.Should().HaveCount(2);
        model.BlokNpcTotalen[(Dag.Vrijdag, StarttijdIds.Tijd1030)].DirectMinimum.Should().Be(3);
        model.BlokNpcTotalen[(Dag.Zaterdag, StarttijdIds.Tijd1030)].DirectMinimum.Should().Be(5);
    }

    [Test]
    public void TweeBlokken_creates_overflow_in_next_block()
    {
        var model = CreateModel();
        model.PlotboekPaginas =
        [
            CreatePagina(Dag.Vrijdag, StarttijdIds.Tijd1030, npcsMin: 4, aantalBlokken: AantalBlokkenIds.TweeBlokken),
        ];

        model.CalculateBlokNpcTotalen();

        model.BlokNpcTotalen.Should().ContainKey((Dag.Vrijdag, StarttijdIds.Tijd1030));

        var overflowKeys = model.BlokNpcTotalen.Keys
            .Where(k => k.Item1 == Dag.Vrijdag && k.Item2 != StarttijdIds.Tijd1030)
            .ToList();
        overflowKeys.Should().NotBeEmpty();

        var overflowBlock = model.BlokNpcTotalen[overflowKeys[0]];
        overflowBlock.OverflowVanVorigeBlokken.Should().NotBeEmpty();
        overflowBlock.OverflowVanVorigeBlokken[0].Minimum.Should().Be(4);
        overflowBlock.OverflowVanVorigeBlokken[0].VanStarttijdId.Should().Be(StarttijdIds.Tijd1030);
    }

    [Test]
    public void Overflow_merges_with_existing_block_totals()
    {
        var model = CreateModel();
        model.PlotboekPaginas =
        [
            CreatePagina(Dag.Vrijdag, StarttijdIds.Tijd1030, npcsMin: 4, aantalBlokken: AantalBlokkenIds.TweeBlokken),
            CreatePagina(Dag.Vrijdag, StarttijdIds.Tijd1230, npcsMin: 2),
        ];

        model.CalculateBlokNpcTotalen();

        var block1230 = model.BlokNpcTotalen[(Dag.Vrijdag, StarttijdIds.Tijd1230)];
        block1230.DirectMinimum.Should().Be(2);
        block1230.OverflowVanVorigeBlokken.Should().NotBeEmpty();
        block1230.TotaalMinimum.Should().Be(6);
    }

    [Test]
    public void DrieBlokken_overflows_into_exactly_the_next_two_slots()
    {
        var model = CreateModel();
        model.PlotboekPaginas =
        [
            CreatePagina(Dag.Vrijdag, StarttijdIds.Tijd1030, npcsMin: 4, aantalBlokken: AantalBlokkenIds.DrieBlokken),
        ];

        model.CalculateBlokNpcTotalen();

        model.BlokNpcTotalen.Keys.Should().BeEquivalentTo(
            [
                (Dag.Vrijdag, StarttijdIds.Tijd1030),
                (Dag.Vrijdag, StarttijdIds.Tijd1230),
                (Dag.Vrijdag, StarttijdIds.Tijd1430),
            ],
            "a three-block encounter occupies its own slot plus the two following ones, by Order");
    }

    [Test]
    public void Overflow_stops_at_the_last_slot_of_the_day()
    {
        var model = CreateModel();
        model.PlotboekPaginas =
        [
            CreatePagina(Dag.Vrijdag, StarttijdIds.Tijd2200, npcsMin: 4, aantalBlokken: AantalBlokkenIds.DrieBlokken),
        ];

        model.CalculateBlokNpcTotalen();

        model.BlokNpcTotalen.Keys.Should().BeEquivalentTo(
            [
                (Dag.Vrijdag, StarttijdIds.Tijd2200),
                (Dag.Vrijdag, StarttijdIds.Tijd0000),
            ],
            "there is no slot after 00:00, so the span is simply shorter");
    }

    [Test]
    public void Overflow_is_skipped_for_an_unknown_aantal_blokken()
    {
        var model = CreateModel();
        model.PlotboekPaginas =
        [
            CreatePagina(Dag.Vrijdag, StarttijdIds.Tijd1030, npcsMin: 4, aantalBlokken: "Onbekend"),
        ];

        model.CalculateBlokNpcTotalen();

        model.BlokNpcTotalen.Keys.Should().BeEquivalentTo([(Dag.Vrijdag, StarttijdIds.Tijd1030)]);
    }

    [Test]
    public void Overflow_skips_a_slot_that_does_not_exist_on_that_day()
    {
        var model = CreateModelMetZondagSlots();
        model.PlotboekPaginas =
        [
            CreatePagina(Dag.Zaterdag, StarttijdIds.Tijd1030, npcsMin: 4, aantalBlokken: AantalBlokkenIds.TweeBlokken),
        ];

        model.CalculateBlokNpcTotalen();

        model.BlokNpcTotalen.Keys.Should().BeEquivalentTo(
            [
                (Dag.Zaterdag, StarttijdIds.Tijd1030),
                (Dag.Zaterdag, StarttijdIds.Tijd1230),
            ],
            "11:30 only exists on the last day, so a Saturday encounter runs over into 12:30");
    }

    [Test]
    public void Overflow_on_the_last_day_uses_that_day_s_own_slots()
    {
        var model = CreateModelMetZondagSlots();
        model.PlotboekPaginas =
        [
            CreatePagina(Dag.Zondag, StarttijdIds.Tijd1130, npcsMin: 4, aantalBlokken: AantalBlokkenIds.TweeBlokken),
        ];

        model.CalculateBlokNpcTotalen();

        model.BlokNpcTotalen.Keys.Should().BeEquivalentTo(
            [
                (Dag.Zondag, StarttijdIds.Tijd1130),
                (Dag.Zondag, StarttijdIds.Tijd1330),
            ],
            "the last day runs 11:30 then 13:30, skipping the slots switched off for that day");
    }

    [Test]
    public void Overflow_from_the_last_block_of_the_last_day_goes_nowhere()
    {
        var model = CreateModelMetZondagSlots();
        model.PlotboekPaginas =
        [
            CreatePagina(Dag.Zondag, StarttijdIds.Tijd1330, npcsMin: 4, aantalBlokken: AantalBlokkenIds.TweeBlokken),
        ];

        model.CalculateBlokNpcTotalen();

        model.BlokNpcTotalen.Keys.Should().BeEquivalentTo(
            [(Dag.Zondag, StarttijdIds.Tijd1330)],
            "the day ends after the eindscène, so there is nothing left to overflow into");
    }

    [Test]
    public void Overflow_from_a_slot_that_is_no_longer_available_stays_on_its_own_slot()
    {
        var model = CreateModelMetZondagSlots();
        model.PlotboekPaginas =
        [
            CreatePagina(Dag.Zondag, StarttijdIds.Tijd1430, npcsMin: 4, aantalBlokken: AantalBlokkenIds.TweeBlokken),
        ];

        model.CalculateBlokNpcTotalen();

        model.BlokNpcTotalen.Keys.Should().BeEquivalentTo(
            [(Dag.Zondag, StarttijdIds.Tijd1430)],
            "a plot left behind on a disabled slot has no place in that day's block order");
    }

    [Test]
    public void Overflow_stays_put_for_a_page_on_a_day_the_plotboek_no_longer_covers()
    {
        var model = CreateModelMetZondagSlots();
        model.PlotboekPaginas =
        [
            CreatePagina(Dag.Donderdag, StarttijdIds.Tijd1030, npcsMin: 4, aantalBlokken: AantalBlokkenIds.TweeBlokken),
        ];

        model.CalculateBlokNpcTotalen();

        model.BlokNpcTotalen.Keys.Should().BeEquivalentTo(
            [(Dag.Donderdag, StarttijdIds.Tijd1030)],
            "shortening the day range leaves a page on a day with no day type, and guessing its blocks from "
                + "every day type at once is what put a last-day 11:30 on a Saturday");
    }

    /// <summary>
    /// A Friday-Sunday plotboek after the last day moved to two blocks: 11:30 and 13:30 are last-day only,
    /// 10:30/12:30/14:30 are gone from the last day but still run on the middle days.
    /// </summary>
    private static PlotboekModel CreateModelMetZondagSlots()
    {
        var model = CreateModel();
        model.AvailableDays = [Dag.Vrijdag, Dag.Zaterdag, Dag.Zondag];
        model.AvailableStarttijdConfigs =
        [
            .. model.AvailableStarttijdConfigs.Select(cfg => cfg.Id switch
            {
                StarttijdIds.Tijd1030 => cfg with { IsAvailableOnLaatsteDag = false, Order = 1 },
                StarttijdIds.Tijd1230 => cfg with { IsAvailableOnLaatsteDag = false, Order = 3 },
                StarttijdIds.Tijd1430 => cfg with { IsAvailableOnLaatsteDag = false, Order = 5 },
                StarttijdIds.Tijd1630 => cfg with { Order = 6 },
                StarttijdIds.Tijd1930Introquest => cfg with { Order = 7 },
                StarttijdIds.Tijd2000 => cfg with { Order = 8 },
                StarttijdIds.Tijd2200 => cfg with { Order = 9 },
                StarttijdIds.Tijd0000 => cfg with { Order = 10 },
                _ => cfg,
            }),
            new StarttijdConfig { Id = StarttijdIds.Tijd1130, EnumValue = 9, DisplayName = "11:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = false, IsAvailableOnLaatsteDag = true, Order = 2, BlokVensterLaatsteDag = "11:30 - 13:30" },
            new StarttijdConfig { Id = StarttijdIds.Tijd1330, EnumValue = 10, DisplayName = "13:30: Eindscène", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = false, IsAvailableOnLaatsteDag = true, Order = 4, BlokVensterLaatsteDag = "13:30 - 15:30" },
        ];

        return model;
    }

    private static PlotboekModel.PlotboekPaginaInfo CreatePagina(
        Dag dag,
        string starttijdId,
        int npcsMin = 0,
        int? npcsMax = null,
        bool alleNpcs = false,
        bool isSpelerGedreven = false,
        string aantalBlokken = AantalBlokkenIds.EenBlok)
    {
        var rollen = npcsMin > 0
            ? [NpcRolData.Create("NPC", npcsMin, npcsMax, false, false, null, false, null, null)]
            : Array.Empty<NpcRolData>();

        return new PlotboekModel.PlotboekPaginaInfo
        {
            Id = PlotboekPaginaId.Next(),
            Title = "Test",
            Dag = dag,
            StarttijdId = starttijdId,
            Order = 1,
            AantalBlokkenId = aantalBlokken,
            AantalNpcs = AantalNpcsData.Create(rollen, alleNpcs),
            IsSpelerGedreven = isSpelerGedreven,
        };
    }
}

