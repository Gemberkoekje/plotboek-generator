namespace PlotboekGenerator.test.Pages;

/// <summary>
/// The dropdown labels and the per-day option payload the plot page hands to starttijd.js. The client
/// has no rules of its own any more: whatever these produce is exactly what the select shows.
/// </summary>
public sealed class PlotboekModel_StarttijdOpties_specs
{
    [Test]
    public void Dropdown_label_separates_the_block_with_a_middle_dot()
    {
        var result = PlotboekModel.GetStarttijdDropdownLabel(Dag.Vrijdag, StarttijdIds.Tijd2000, Dag.Vrijdag, Dag.Zondag, CreateStarttijdConfigs());

        result.Should().Be("20:00 · Blok 1.1: 18:00 - 21:00");
    }

    [Test]
    public void Dropdown_label_uses_the_day_number_of_that_day()
    {
        var result = PlotboekModel.GetStarttijdDropdownLabel(Dag.Zaterdag, StarttijdIds.Tijd2200, Dag.Vrijdag, Dag.Zondag, CreateStarttijdConfigs());

        result.Should().Be("22:00 · Blok 2.6: 21:00 - 23:00");
    }

    [Test]
    public void Dropdown_label_of_a_slot_without_a_block_is_just_the_name()
    {
        var result = PlotboekModel.GetStarttijdDropdownLabel(Dag.Vrijdag, StarttijdIds.Tijd1930Introquest, Dag.Vrijdag, Dag.Zondag, CreateStarttijdConfigs());

        result.Should().Be("19:30: Introquest", because: "the introquest slot has no block window, so it carries no block number either");
    }

    [Test]
    public void Dropdown_label_of_none_is_just_the_placeholder()
    {
        var result = PlotboekModel.GetStarttijdDropdownLabel(Dag.Zaterdag, StarttijdIds.None, Dag.Vrijdag, Dag.Zondag, CreateStarttijdConfigs());

        result.Should().Be("-");
    }

    [Test]
    public void Dropdown_label_marks_a_slot_that_is_switched_off_for_that_day()
    {
        var result = PlotboekModel.GetStarttijdDropdownLabel(Dag.Zondag, StarttijdIds.Tijd1430, Dag.Vrijdag, Dag.Zondag, CreateZondagStarttijdConfigs());

        result.Should().Be("14:30 (niet beschikbaar op deze dag)", because: "a plot left behind on a disabled slot has to be moved, and the dropdown says so");
    }

    [Test]
    public void Opties_are_keyed_by_the_day_the_day_select_offers()
    {
        var result = BuildOptiesPerDag(CreateStarttijdConfigs(), Dag.Zaterdag, StarttijdIds.Tijd1030);

        result.Keys.Should().BeEquivalentTo(["None", "Vrijdag", "Zaterdag", "Zondag"]);
    }

    [Test]
    public void Opties_for_no_day_hold_only_none()
    {
        var result = BuildOptiesPerDag(CreateStarttijdConfigs(), Dag.Zaterdag, StarttijdIds.Tijd1030);

        result["None"].Select(o => o.Id).Should().BeEquivalentTo([StarttijdIds.None]);
    }

    [Test]
    public void Opties_per_day_match_what_the_server_renders_for_that_day()
    {
        var result = BuildOptiesPerDag(CreateStarttijdConfigs(), Dag.Zaterdag, StarttijdIds.Tijd1030);

        result["Vrijdag"].Select(o => o.Id).Should().BeEquivalentTo([
            StarttijdIds.None,
            StarttijdIds.Tijd1930Introquest,
            StarttijdIds.Tijd2000,
            StarttijdIds.Tijd2200,
            StarttijdIds.Tijd0000,
        ]);
    }

    [Test]
    public void Opties_label_each_slot_with_the_block_of_its_own_day()
    {
        var result = BuildOptiesPerDag(CreateStarttijdConfigs(), Dag.Zaterdag, StarttijdIds.Tijd1030);

        result["Zondag"].Should().ContainSingle(o => o.Id == StarttijdIds.Tijd1430)
            .Which.Label.Should().Be("14:30 · Blok 3.3: 13:30 - 15:30");
    }

    [Test]
    public void Opties_keep_the_current_slot_on_the_day_the_encounter_is_on()
    {
        var result = BuildOptiesPerDag(CreateZondagStarttijdConfigs(), Dag.Zondag, StarttijdIds.Tijd1430);

        string[] verwacht = [StarttijdIds.None, StarttijdIds.Tijd1130, StarttijdIds.Tijd1330, StarttijdIds.Tijd1430];

        result["Zondag"].Select(o => o.Id).Should().BeEquivalentTo(verwacht, because: "the select would otherwise fall back to None and the next save would drop the starttijd");
    }

    [Test]
    public void Opties_do_not_offer_the_current_slot_on_other_days()
    {
        var result = BuildOptiesPerDag(CreateZondagStarttijdConfigs(), Dag.Zaterdag, StarttijdIds.Tijd1630);

        string[] verwacht = [StarttijdIds.None, StarttijdIds.Tijd1130, StarttijdIds.Tijd1330];

        result["Zondag"].Select(o => o.Id).Should().BeEquivalentTo(verwacht, because: "moving the plot to another day is a deliberate choice; it does not carry a slot that does not exist there");
    }

    [Test]
    public void Json_names_the_fields_starttijd_js_reads()
    {
        var result = CreateModel(Dag.Zaterdag, StarttijdIds.Tijd1030).BuildStarttijdOptiesJson();

        result.Should().Contain("\"id\":").And.Contain("\"label\":", because: "starttijd.js reads optie.id and optie.label");
    }

    [Test]
    public void Json_is_keyed_by_the_value_the_day_option_carries()
    {
        var result = CreateModel(Dag.Zaterdag, StarttijdIds.Tijd1030).BuildStarttijdOptiesJson();

        result.Should().Contain("\"Zaterdag\":", because: "the client looks the list up by the day select's value");
    }

    [Test]
    public void Json_cannot_break_out_of_the_script_element()
    {
        var model = CreateModel(Dag.Zaterdag, StarttijdIds.Tijd1030);
        model.AvailableStarttijdConfigs =
        [
            .. model.AvailableStarttijdConfigs.Select(cfg => cfg.Id == StarttijdIds.Tijd1030
                ? cfg with { DisplayName = "10:30 </script><script>alert(1)</script>" }
                : cfg),
        ];

        var result = model.BuildStarttijdOptiesJson();

        result.Should().NotContain("</script>", because: "the payload is written with Html.Raw into a script element");
    }

    private static PlotboekModel CreateModel(Dag huidigeDag, string huidigeStarttijdId)
    {
        var model = new PlotboekModel(null!, null!, null!, null!, null!, null!, null!, null!, null!)
        {
            AvailableDays = [Dag.None, Dag.Vrijdag, Dag.Zaterdag, Dag.Zondag],
            AvailableStarttijdConfigs = [.. CreateStarttijdConfigs()],
            EditableDay = huidigeDag,
            EditableStarttijd = huidigeStarttijdId,
            PlotboekSettings = new PlotboekModel.PlotboekSettingsInfo
            {
                StartDag = Dag.Vrijdag,
                EindDag = Dag.Zondag,
            },
        };

        return model;
    }

    private static Dictionary<string, List<PlotboekModel.StarttijdOptie>> BuildOptiesPerDag(
        IReadOnlyList<StarttijdConfig> configs,
        Dag huidigeDag,
        string huidigeStarttijdId) =>
        PlotboekModel.BuildStarttijdOptiesPerDag(
            [Dag.None, Dag.Vrijdag, Dag.Zaterdag, Dag.Zondag],
            Dag.Vrijdag,
            Dag.Zondag,
            configs,
            huidigeDag,
            huidigeStarttijdId);

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
}
