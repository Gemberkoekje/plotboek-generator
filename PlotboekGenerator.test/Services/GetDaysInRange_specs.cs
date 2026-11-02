namespace PlotboekGenerator.test.Services;

public sealed class GetDaysInRange_specs
{
    [Test]
    public void Returns_only_None_when_startDag_is_None()
    {
        var result = PlotboekExportBuilder.GetDaysInRange(Dag.None, Dag.Vrijdag);

        result.Should().BeEquivalentTo([Dag.None]);
    }

    [Test]
    public void Returns_only_None_when_eindDag_is_None()
    {
        var result = PlotboekExportBuilder.GetDaysInRange(Dag.Maandag, Dag.None);

        result.Should().BeEquivalentTo([Dag.None]);
    }

    [Test]
    public void Returns_only_None_when_both_are_None()
    {
        var result = PlotboekExportBuilder.GetDaysInRange(Dag.None, Dag.None);

        result.Should().BeEquivalentTo([Dag.None]);
    }

    [Test]
    public void Returns_single_day_when_start_equals_eind()
    {
        var result = PlotboekExportBuilder.GetDaysInRange(Dag.Woensdag, Dag.Woensdag);

        result.Should().BeEquivalentTo([Dag.None, Dag.Woensdag]);
    }

    [Test]
    public void Returns_consecutive_weekdays()
    {
        var result = PlotboekExportBuilder.GetDaysInRange(Dag.Maandag, Dag.Vrijdag);

        result.Should().BeEquivalentTo([Dag.None, Dag.Maandag, Dag.Dinsdag, Dag.Woensdag, Dag.Donderdag, Dag.Vrijdag]);
    }

    [Test]
    public void Wraps_around_from_Zondag_to_Maandag()
    {
        var result = PlotboekExportBuilder.GetDaysInRange(Dag.Zaterdag, Dag.Dinsdag);

        result.Should().BeEquivalentTo([Dag.None, Dag.Zaterdag, Dag.Zondag, Dag.Maandag, Dag.Dinsdag]);
    }

    [Test]
    public void Returns_full_week_from_Maandag_to_Zondag()
    {
        var result = PlotboekExportBuilder.GetDaysInRange(Dag.Maandag, Dag.Zondag);

        result.Should().BeEquivalentTo([Dag.None, Dag.Maandag, Dag.Dinsdag, Dag.Woensdag, Dag.Donderdag, Dag.Vrijdag, Dag.Zaterdag, Dag.Zondag]);
    }

    [Test]
    public void Always_includes_None_as_first_element()
    {
        var result = PlotboekExportBuilder.GetDaysInRange(Dag.Donderdag, Dag.Zaterdag);

        result[0].Should().Be(Dag.None);
    }

    [Test]
    public void Returns_weekend_days()
    {
        var result = PlotboekExportBuilder.GetDaysInRange(Dag.Vrijdag, Dag.Zondag);

        result.Should().BeEquivalentTo([Dag.None, Dag.Vrijdag, Dag.Zaterdag, Dag.Zondag]);
    }
}

