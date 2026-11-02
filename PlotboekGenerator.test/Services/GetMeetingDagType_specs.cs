namespace PlotboekGenerator.test.Services;

public sealed class GetMeetingDagType_specs
{
    [Test]
    public void First_day_returns_EersteDag()
    {
        var days = new[] { Dag.Vrijdag, Dag.Zaterdag, Dag.Zondag };
        PlotboekExportBuilder.GetMeetingDagType(Dag.Vrijdag, days).Should().Be(MeetingDagType.EersteDag);
    }

    [Test]
    public void Last_day_returns_LaatseDag()
    {
        var days = new[] { Dag.Vrijdag, Dag.Zaterdag, Dag.Zondag };
        PlotboekExportBuilder.GetMeetingDagType(Dag.Zondag, days).Should().Be(MeetingDagType.LaatseDag);
    }

    [Test]
    public void Middle_day_returns_Tussendag()
    {
        var days = new[] { Dag.Vrijdag, Dag.Zaterdag, Dag.Zondag };
        PlotboekExportBuilder.GetMeetingDagType(Dag.Zaterdag, days).Should().Be(MeetingDagType.Tussendag);
    }

    [Test]
    public void Single_day_is_both_first_and_last_returns_EersteDag()
    {
        var days = new[] { Dag.Zaterdag };
        PlotboekExportBuilder.GetMeetingDagType(Dag.Zaterdag, days).Should().Be(MeetingDagType.EersteDag);
    }

    [Test]
    public void Day_not_in_range_returns_Tussendag()
    {
        var days = new[] { Dag.Vrijdag, Dag.Zaterdag, Dag.Zondag };
        PlotboekExportBuilder.GetMeetingDagType(Dag.Maandag, days).Should().Be(MeetingDagType.Tussendag);
    }
}

