namespace PlotboekGenerator.test.Pages;

public sealed class PlotboekModel_GetAvailableStarttijden_specs
{
    [Test]
    public void None_selected_returns_only_none()
    {
        var result = PlotboekModel.GetAvailableStarttijden(Dag.None, Dag.Vrijdag, Dag.Zondag, CreateStarttijdConfigs());

        result.Should().BeEquivalentTo([StarttijdIds.None]);
    }

    [Test]
    public void First_day_returns_evening_times()
    {
        var result = PlotboekModel.GetAvailableStarttijden(Dag.Vrijdag, Dag.Vrijdag, Dag.Zondag, CreateStarttijdConfigs());

        result.Should().BeEquivalentTo([
            StarttijdIds.None,
            StarttijdIds.Tijd1930Introquest,
            StarttijdIds.Tijd2000,
            StarttijdIds.Tijd2200,
            StarttijdIds.Tijd0000,
        ]);
    }

    [Test]
    public void Last_day_returns_morning_times()
    {
        var result = PlotboekModel.GetAvailableStarttijden(Dag.Zondag, Dag.Vrijdag, Dag.Zondag, CreateStarttijdConfigs());

        result.Should().BeEquivalentTo([
            StarttijdIds.None,
            StarttijdIds.Tijd1030,
            StarttijdIds.Tijd1230,
            StarttijdIds.Tijd1430,
        ]);
    }

    [Test]
    public void Middle_day_returns_all_times()
    {
        var result = PlotboekModel.GetAvailableStarttijden(Dag.Zaterdag, Dag.Vrijdag, Dag.Zondag, CreateStarttijdConfigs());

        result.Should().BeEquivalentTo([
            StarttijdIds.None,
            StarttijdIds.Tijd1030,
            StarttijdIds.Tijd1230,
            StarttijdIds.Tijd1430,
            StarttijdIds.Tijd1630,
            StarttijdIds.Tijd2000,
            StarttijdIds.Tijd2200,
            StarttijdIds.Tijd0000,
        ]);
    }

    [Test]
    public void Single_day_event_returns_evening_times()
    {
        var result = PlotboekModel.GetAvailableStarttijden(Dag.Zaterdag, Dag.Zaterdag, Dag.Zaterdag, CreateStarttijdConfigs());

        result.Should().BeEquivalentTo([
            StarttijdIds.None,
            StarttijdIds.Tijd1930Introquest,
            StarttijdIds.Tijd2000,
            StarttijdIds.Tijd2200,
            StarttijdIds.Tijd0000,
        ]);
    }

    [Test]
    public void Keeps_the_current_slot_when_it_is_not_available_on_that_day()
    {
        var result = PlotboekModel.GetAvailableStarttijden(
            Dag.Zondag, Dag.Vrijdag, Dag.Zondag, CreateZondagStarttijdConfigs(), StarttijdIds.Tijd1430);

        result.Should().BeEquivalentTo(
            [
                StarttijdIds.None,
                StarttijdIds.Tijd1130,
                StarttijdIds.Tijd1330,
                StarttijdIds.Tijd1430,
            ],
            "a plot left behind on a disabled slot must keep it, or saving would silently clear it");
    }

    [Test]
    public void Puts_the_kept_slot_in_its_own_place_in_the_order()
    {
        var result = PlotboekModel.GetAvailableStarttijden(
            Dag.Zondag, Dag.Vrijdag, Dag.Zondag, CreateZondagStarttijdConfigs(), StarttijdIds.Tijd1030);

        result.Should().ContainInOrder(
            StarttijdIds.None,
            StarttijdIds.Tijd1030,
            StarttijdIds.Tijd1130,
            StarttijdIds.Tijd1330);
    }

    [Test]
    public void Does_not_duplicate_a_current_slot_that_is_available()
    {
        var result = PlotboekModel.GetAvailableStarttijden(
            Dag.Zondag, Dag.Vrijdag, Dag.Zondag, CreateZondagStarttijdConfigs(), StarttijdIds.Tijd1130);

        result.Should().BeEquivalentTo([StarttijdIds.None, StarttijdIds.Tijd1130, StarttijdIds.Tijd1330]);
    }

    [TestCase(StarttijdIds.None)]
    [TestCase("")]
    [TestCase("Onbekend")]
    public void Adds_nothing_for_a_current_slot_that_is_none_or_unknown(string huidigeStarttijdId)
    {
        var result = PlotboekModel.GetAvailableStarttijden(
            Dag.Zondag, Dag.Vrijdag, Dag.Zondag, CreateZondagStarttijdConfigs(), huidigeStarttijdId);

        result.Should().BeEquivalentTo([StarttijdIds.None, StarttijdIds.Tijd1130, StarttijdIds.Tijd1330]);
    }

    [Test]
    public void Keeps_the_current_slot_even_when_the_page_has_no_day()
    {
        var result = PlotboekModel.GetAvailableStarttijden(
            Dag.None, Dag.Vrijdag, Dag.Zondag, CreateStarttijdConfigs(), StarttijdIds.Tijd1430);

        result.Should().BeEquivalentTo([StarttijdIds.None, StarttijdIds.Tijd1430]);
    }

    [Test]
    public void Marks_a_slot_that_is_not_available_on_that_day()
    {
        var result = PlotboekModel.IsStarttijdNietBeschikbaarOpDag(
            Dag.Zondag, StarttijdIds.Tijd1430, Dag.Vrijdag, Dag.Zondag, CreateZondagStarttijdConfigs());

        result.Should().BeTrue();
    }

    [TestCase(StarttijdIds.Tijd1130)]
    [TestCase(StarttijdIds.None)]
    [TestCase("Onbekend")]
    public void Does_not_mark_an_available_none_or_unknown_slot(string starttijdId)
    {
        var result = PlotboekModel.IsStarttijdNietBeschikbaarOpDag(
            Dag.Zondag, starttijdId, Dag.Vrijdag, Dag.Zondag, CreateZondagStarttijdConfigs());

        result.Should().BeFalse();
    }

    private static IReadOnlyList<StarttijdConfig> CreateZondagStarttijdConfigs() =>
    [
        .. CreateStarttijdConfigs().Select(cfg => cfg.Id switch
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

    private static IReadOnlyList<StarttijdConfig> CreateStarttijdConfigs() =>
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
}

