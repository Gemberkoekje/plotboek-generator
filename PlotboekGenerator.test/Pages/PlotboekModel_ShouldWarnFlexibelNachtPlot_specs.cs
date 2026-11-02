namespace PlotboekGenerator.test.Pages;

public sealed class PlotboekModel_ShouldWarnFlexibelNachtPlot_specs
{
    [TestCase(StarttijdIds.Tijd2000)]
    [TestCase(StarttijdIds.Tijd2200)]
    [TestCase(StarttijdIds.Tijd0000)]
    public void No_warning_when_in_a_night_slot(string starttijdId)
    {
        var warn = PlotboekModel.ShouldWarnFlexibelNachtPlot(isFlexibelNachtPlot: true, isSpelerGedreven: false, starttijdId);

        warn.Should().BeFalse();
    }

    [TestCase(StarttijdIds.None)]
    [TestCase(StarttijdIds.Tijd1030)]
    [TestCase(StarttijdIds.Tijd1630)]
    [TestCase(StarttijdIds.Tijd1930Introquest)]
    public void Warns_when_not_in_a_20h_or_later_slot(string starttijdId)
    {
        var warn = PlotboekModel.ShouldWarnFlexibelNachtPlot(isFlexibelNachtPlot: true, isSpelerGedreven: false, starttijdId);

        warn.Should().BeTrue();
    }

    [Test]
    public void No_warning_when_option_is_off()
    {
        var warn = PlotboekModel.ShouldWarnFlexibelNachtPlot(isFlexibelNachtPlot: false, isSpelerGedreven: false, StarttijdIds.Tijd1030);

        warn.Should().BeFalse();
    }

    [Test]
    public void No_warning_when_speler_gedreven_since_slot_is_unused()
    {
        var warn = PlotboekModel.ShouldWarnFlexibelNachtPlot(isFlexibelNachtPlot: true, isSpelerGedreven: true, StarttijdIds.None);

        warn.Should().BeFalse();
    }
}
