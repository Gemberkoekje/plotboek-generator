namespace PlotboekGenerator.test.Pages;

public sealed class PlotboekModel_GetStarttijdSummaryLabel_specs
{
    [Test]
    public void First_day_evening_slot_includes_block_window()
    {
        var result = PlotboekModel.GetStarttijdSummaryLabel(Dag.Vrijdag, StarttijdIds.Tijd2000, Dag.Vrijdag, Dag.Zondag, CreateStarttijdConfigs());

        result.Should().Be("20:00 [Blok 1.1: 18:00 - 21:00]");
    }

    [Test]
    public void Middle_day_night_slot_includes_block_window()
    {
        var result = PlotboekModel.GetStarttijdSummaryLabel(Dag.Zaterdag, StarttijdIds.Tijd2200, Dag.Vrijdag, Dag.Zondag, CreateStarttijdConfigs());

        result.Should().Be("22:00 [Blok 2.6: 21:00 - 23:00]");
    }

    [Test]
    public void Last_day_morning_slot_includes_block_window()
    {
        var result = PlotboekModel.GetStarttijdSummaryLabel(Dag.Zondag, StarttijdIds.Tijd1430, Dag.Vrijdag, Dag.Zondag, CreateStarttijdConfigs());

        result.Should().Be("14:30 [Blok 3.3: 13:30 - 15:30]");
    }

    [Test]
    public void Introquest_slot_keeps_original_label()
    {
        var result = PlotboekModel.GetStarttijdSummaryLabel(Dag.Vrijdag, StarttijdIds.Tijd1930Introquest, Dag.Vrijdag, Dag.Zondag, CreateStarttijdConfigs());

        result.Should().Be("19:30: Introquest");
    }

    [Test]
    public void Slot_not_available_on_that_day_keeps_original_label()
    {
        var result = PlotboekModel.GetStarttijdSummaryLabel(Dag.Vrijdag, StarttijdIds.Tijd1030, Dag.Vrijdag, Dag.Zondag, CreateStarttijdConfigs());

        result.Should().Be("10:30", because: "10:30 is not selectable on the first day, so it has no block there");
    }

    [Test]
    public void Slot_without_a_window_does_not_consume_a_block_number()
    {
        var configs = CreateStarttijdConfigs()
            .Select(c => c.Id == StarttijdIds.Tijd1630 ? c with { BlokVensterMiddelDagen = string.Empty } : c)
            .ToList();

        var result = PlotboekModel.GetStarttijdSummaryLabel(Dag.Zaterdag, StarttijdIds.Tijd2000, Dag.Vrijdag, Dag.Zondag, configs);

        result.Should().Be("20:00 [Blok 2.4: 19:00 - 21:00]", because: "16:30 lost its window, so the slots after it all move up one number");
    }

    [Test]
    public void Last_day_slot_added_in_admin_gets_the_first_block_number()
    {
        var result = PlotboekModel.GetStarttijdSummaryLabel(Dag.Zondag, StarttijdIds.Tijd1130, Dag.Vrijdag, Dag.Zondag, CreateZondagStarttijdConfigs());

        result.Should().Be("11:30 [Blok 3.1: 11:30 - 13:30]");
    }

    [Test]
    public void Last_day_eindscene_slot_gets_the_second_block_number()
    {
        var result = PlotboekModel.GetStarttijdSummaryLabel(Dag.Zondag, StarttijdIds.Tijd1330, Dag.Vrijdag, Dag.Zondag, CreateZondagStarttijdConfigs());

        result.Should().Be("13:30: Eindscène [Blok 3.2: 13:30 - 15:30]");
    }

    [Test]
    public void Slot_switched_off_for_the_last_day_keeps_original_label()
    {
        var result = PlotboekModel.GetStarttijdSummaryLabel(Dag.Zondag, StarttijdIds.Tijd1430, Dag.Vrijdag, Dag.Zondag, CreateZondagStarttijdConfigs());

        result.Should().Be("14:30", because: "a plot left behind on a disabled slot still shows its name");
    }

    /// <summary>The slots as they are seeded today: the last day runs 10:30 - 12:30 - 14:30.</summary>
    private static IReadOnlyList<StarttijdConfig> CreateStarttijdConfigs() =>
    [
        new StarttijdConfig { Id = StarttijdIds.None, EnumValue = 0, DisplayName = "-", IsNone = true, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = false, IsAvailableOnLaatsteDag = false, Order = 0 },
        new StarttijdConfig { Id = StarttijdIds.Tijd1030, EnumValue = 1, DisplayName = "10:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = true, Order = 1, BlokVensterMiddelDagen = "9:30 - 11:30", BlokVensterLaatsteDag = "9:30 - 11:30" },
        new StarttijdConfig { Id = StarttijdIds.Tijd1230, EnumValue = 2, DisplayName = "12:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = true, Order = 2, BlokVensterMiddelDagen = "11:30 - 13:30", BlokVensterLaatsteDag = "11:30 - 13:30" },
        new StarttijdConfig { Id = StarttijdIds.Tijd1430, EnumValue = 3, DisplayName = "14:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = true, Order = 3, BlokVensterMiddelDagen = "13:30 - 15:30", BlokVensterLaatsteDag = "13:30 - 15:30" },
        new StarttijdConfig { Id = StarttijdIds.Tijd1630, EnumValue = 4, DisplayName = "16:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = false, Order = 4, BlokVensterMiddelDagen = "15:30 - 17:30" },
        new StarttijdConfig { Id = StarttijdIds.Tijd1930Introquest, EnumValue = 5, DisplayName = "19:30: Introquest", IsNone = false, IsAvailableOnEersteDag = true, IsAvailableOnMiddelDagen = false, IsAvailableOnLaatsteDag = false, Order = 5 },
        new StarttijdConfig { Id = StarttijdIds.Tijd2000, EnumValue = 6, DisplayName = "20:00", IsNone = false, IsAvailableOnEersteDag = true, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = false, Order = 6, BlokVensterEersteDag = "18:00 - 21:00", BlokVensterMiddelDagen = "19:00 - 21:00" },
        new StarttijdConfig { Id = StarttijdIds.Tijd2200, EnumValue = 7, DisplayName = "22:00", IsNone = false, IsAvailableOnEersteDag = true, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = false, Order = 7, BlokVensterEersteDag = "21:00 - 23:00", BlokVensterMiddelDagen = "21:00 - 23:00" },
        new StarttijdConfig { Id = StarttijdIds.Tijd0000, EnumValue = 8, DisplayName = "00:00", IsNone = false, IsAvailableOnEersteDag = true, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = false, Order = 8, BlokVensterEersteDag = "23:00 - 1:00", BlokVensterMiddelDagen = "23:00 - 1:00" },
    ];

    /// <summary>
    /// The slots after the admin moves the last day to two blocks: 11:30 and 13:30 on, 10:30/12:30/14:30 off.
    /// Orders are the ones the time-derived resequencing produces.
    /// </summary>
    private static IReadOnlyList<StarttijdConfig> CreateZondagStarttijdConfigs() =>
    [
        new StarttijdConfig { Id = StarttijdIds.None, EnumValue = 0, DisplayName = "-", IsNone = true, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = false, IsAvailableOnLaatsteDag = false, Order = 0 },
        new StarttijdConfig { Id = StarttijdIds.Tijd1030, EnumValue = 1, DisplayName = "10:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = false, Order = 1, BlokVensterMiddelDagen = "9:30 - 11:30", BlokVensterLaatsteDag = "9:30 - 11:30" },
        new StarttijdConfig { Id = StarttijdIds.Tijd1130, EnumValue = 9, DisplayName = "11:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = false, IsAvailableOnLaatsteDag = true, Order = 2, BlokVensterLaatsteDag = "11:30 - 13:30" },
        new StarttijdConfig { Id = StarttijdIds.Tijd1230, EnumValue = 2, DisplayName = "12:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = false, Order = 3, BlokVensterMiddelDagen = "11:30 - 13:30", BlokVensterLaatsteDag = "11:30 - 13:30" },
        new StarttijdConfig { Id = StarttijdIds.Tijd1330, EnumValue = 10, DisplayName = "13:30: Eindscène", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = false, IsAvailableOnLaatsteDag = true, Order = 4, BlokVensterLaatsteDag = "13:30 - 15:30" },
        new StarttijdConfig { Id = StarttijdIds.Tijd1430, EnumValue = 3, DisplayName = "14:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = false, Order = 5, BlokVensterMiddelDagen = "13:30 - 15:30", BlokVensterLaatsteDag = "13:30 - 15:30" },
        new StarttijdConfig { Id = StarttijdIds.Tijd1630, EnumValue = 4, DisplayName = "16:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = false, Order = 6, BlokVensterMiddelDagen = "15:30 - 17:30" },
        new StarttijdConfig { Id = StarttijdIds.Tijd1930Introquest, EnumValue = 5, DisplayName = "19:30: Introquest", IsNone = false, IsAvailableOnEersteDag = true, IsAvailableOnMiddelDagen = false, IsAvailableOnLaatsteDag = false, Order = 7 },
        new StarttijdConfig { Id = StarttijdIds.Tijd2000, EnumValue = 6, DisplayName = "20:00", IsNone = false, IsAvailableOnEersteDag = true, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = false, Order = 8, BlokVensterEersteDag = "18:00 - 21:00", BlokVensterMiddelDagen = "19:00 - 21:00" },
        new StarttijdConfig { Id = StarttijdIds.Tijd2200, EnumValue = 7, DisplayName = "22:00", IsNone = false, IsAvailableOnEersteDag = true, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = false, Order = 9, BlokVensterEersteDag = "21:00 - 23:00", BlokVensterMiddelDagen = "21:00 - 23:00" },
        new StarttijdConfig { Id = StarttijdIds.Tijd0000, EnumValue = 8, DisplayName = "00:00", IsNone = false, IsAvailableOnEersteDag = true, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = false, Order = 10, BlokVensterEersteDag = "23:00 - 1:00", BlokVensterMiddelDagen = "23:00 - 1:00" },
    ];
}
