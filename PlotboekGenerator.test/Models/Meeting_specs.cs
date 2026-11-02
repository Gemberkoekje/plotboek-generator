namespace PlotboekGenerator.test.Models;

public sealed class Meeting_specs
{
    [Test]
    public void Create_sets_dag_type()
    {
        var meeting = Meeting.Create(MeetingDagType.EersteDag, "14:00", "Briefing", 1);

        meeting.DagType.Should().Be(MeetingDagType.EersteDag);
    }

    [Test]
    public void Create_sets_tijd()
    {
        var meeting = Meeting.Create(MeetingDagType.Tussendag, "18:30", "Diner", 2);

        meeting.Tijd.Should().Be("18:30");
    }

    [Test]
    public void Create_sets_naam()
    {
        var meeting = Meeting.Create(MeetingDagType.LaatseDag, "10:00", "Afscheid", 3);

        meeting.Naam.Should().Be("Afscheid");
    }

    [Test]
    public void Create_sets_order()
    {
        var meeting = Meeting.Create(MeetingDagType.EersteDag, "09:00", "Ontbijt", 5);

        meeting.Order.Should().Be(5);
    }

    [Test]
    public void Create_generates_unique_ids()
    {
        var a = Meeting.Create(MeetingDagType.EersteDag, "14:00", "Briefing", 1);
        var b = Meeting.Create(MeetingDagType.EersteDag, "14:00", "Briefing", 1);

        a.Id.Should().NotBe(b.Id);
    }

    [Test]
    public void DisplayName_combines_tijd_and_naam()
    {
        var meeting = Meeting.Create(MeetingDagType.Tussendag, "20:00", "Avondprogramma", 1);

        meeting.DisplayName.Should().Be("20:00 Avondprogramma");
    }
}

