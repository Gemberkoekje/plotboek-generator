namespace PlotboekGenerator.test.Services;

public sealed class GetDisplayNumber_specs
{
    [Test]
    public void Returns_plotlijn_number_with_sub_number_for_plotlijn_member()
    {
        var plotlijnId = PlotlijnId.Next();
        var p1 = new TestPage(Dag.Maandag, StarttijdIds.Tijd1030, 1, plotlijnId);
        var p2 = new TestPage(Dag.Maandag, StarttijdIds.Tijd1230, 1, plotlijnId);
        var items = new[] { p1, p2 };

        var plotlijnNumbers = new Dictionary<PlotlijnId, int> { [plotlijnId] = 3 };
        var individualNumbers = new Dictionary<PlotboekPaginaId, int>();

        var result = PlotboekExportBuilder.GetDisplayNumber(
            plotlijnId,
            p1.PaginaId,
            plotlijnNumbers,
            individualNumbers,
            items,
            p => p.PlotlijnId,
            p => p.Dag,
            p => p.StarttijdId,
            p => p.Order,
            p => p.PaginaId);

        result.Should().Be("3.1");
    }

    [Test]
    public void Returns_correct_sub_number_for_second_member_of_plotlijn()
    {
        var plotlijnId = PlotlijnId.Next();
        var p1 = new TestPage(Dag.Maandag, StarttijdIds.Tijd1030, 1, plotlijnId);
        var p2 = new TestPage(Dag.Maandag, StarttijdIds.Tijd1230, 1, plotlijnId);
        var items = new[] { p1, p2 };

        var plotlijnNumbers = new Dictionary<PlotlijnId, int> { [plotlijnId] = 5 };
        var individualNumbers = new Dictionary<PlotboekPaginaId, int>();

        var result = PlotboekExportBuilder.GetDisplayNumber(
            plotlijnId,
            p2.PaginaId,
            plotlijnNumbers,
            individualNumbers,
            items,
            p => p.PlotlijnId,
            p => p.Dag,
            p => p.StarttijdId,
            p => p.Order,
            p => p.PaginaId);

        result.Should().Be("5.2");
    }

    [Test]
    public void Returns_individual_number_for_page_without_plotlijn()
    {
        var p1 = new TestPage(Dag.Maandag, StarttijdIds.Tijd1030, 1);
        var items = new[] { p1 };

        var plotlijnNumbers = new Dictionary<PlotlijnId, int>();
        var individualNumbers = new Dictionary<PlotboekPaginaId, int> { [p1.PaginaId] = 7 };

        var result = PlotboekExportBuilder.GetDisplayNumber(
            default,
            p1.PaginaId,
            plotlijnNumbers,
            individualNumbers,
            items,
            p => p.PlotlijnId,
            p => p.Dag,
            p => p.StarttijdId,
            p => p.Order,
            p => p.PaginaId);

        result.Should().Be("7");
    }

    [Test]
    public void Returns_null_when_not_found_in_either_dictionary()
    {
        var p1 = new TestPage(Dag.Maandag, StarttijdIds.Tijd1030, 1);
        var items = new[] { p1 };

        var plotlijnNumbers = new Dictionary<PlotlijnId, int>();
        var individualNumbers = new Dictionary<PlotboekPaginaId, int>();

        var result = PlotboekExportBuilder.GetDisplayNumber(
            default,
            p1.PaginaId,
            plotlijnNumbers,
            individualNumbers,
            items,
            p => p.PlotlijnId,
            p => p.Dag,
            p => p.StarttijdId,
            p => p.Order,
            p => p.PaginaId);

        result.Should().BeNull();
    }

    [Test]
    public void Plotlijn_members_are_ordered_by_dag_then_starttijd_then_order()
    {
        var plotlijnId = PlotlijnId.Next();
        var late = new TestPage(Dag.Dinsdag, StarttijdIds.Tijd2000, 1, plotlijnId);
        var early = new TestPage(Dag.Maandag, StarttijdIds.Tijd1030, 1, plotlijnId);
        var mid = new TestPage(Dag.Maandag, StarttijdIds.Tijd1430, 1, plotlijnId);
        var items = new[] { late, early, mid };

        var plotlijnNumbers = new Dictionary<PlotlijnId, int> { [plotlijnId] = 1 };
        var individualNumbers = new Dictionary<PlotboekPaginaId, int>();

        var earlyResult = PlotboekExportBuilder.GetDisplayNumber(
            plotlijnId,
            early.PaginaId,
            plotlijnNumbers,
            individualNumbers,
            items,
            p => p.PlotlijnId,
            p => p.Dag,
            p => p.StarttijdId,
            p => p.Order,
            p => p.PaginaId);
        var midResult = PlotboekExportBuilder.GetDisplayNumber(
            plotlijnId,
            mid.PaginaId,
            plotlijnNumbers,
            individualNumbers,
            items,
            p => p.PlotlijnId,
            p => p.Dag,
            p => p.StarttijdId,
            p => p.Order,
            p => p.PaginaId);
        var lateResult = PlotboekExportBuilder.GetDisplayNumber(
            plotlijnId,
            late.PaginaId,
            plotlijnNumbers,
            individualNumbers,
            items,
            p => p.PlotlijnId,
            p => p.Dag,
            p => p.StarttijdId,
            p => p.Order,
            p => p.PaginaId);

        earlyResult.Should().Be("1.1");
        midResult.Should().Be("1.2");
        lateResult.Should().Be("1.3");
    }

    [Test]
    public void PlotlijnId_without_value_falls_through_to_individual_lookup()
    {
        var p1 = new TestPage(Dag.Maandag, StarttijdIds.Tijd1030, 1);
        var items = new[] { p1 };

        var plotlijnNumbers = new Dictionary<PlotlijnId, int>();
        var individualNumbers = new Dictionary<PlotboekPaginaId, int> { [p1.PaginaId] = 42 };

        var result = PlotboekExportBuilder.GetDisplayNumber(
            default,
            p1.PaginaId,
            plotlijnNumbers,
            individualNumbers,
            items,
            p => p.PlotlijnId,
            p => p.Dag,
            p => p.StarttijdId,
            p => p.Order,
            p => p.PaginaId);

        result.Should().Be("42");
    }

    [Test]
    public void Single_member_plotlijn_returns_number_dot_one()
    {
        var plotlijnId = PlotlijnId.Next();
        var p1 = new TestPage(Dag.Maandag, StarttijdIds.Tijd1030, 1, plotlijnId);
        var items = new[] { p1 };

        var plotlijnNumbers = new Dictionary<PlotlijnId, int> { [plotlijnId] = 2 };
        var individualNumbers = new Dictionary<PlotboekPaginaId, int>();

        var result = PlotboekExportBuilder.GetDisplayNumber(
            plotlijnId,
            p1.PaginaId,
            plotlijnNumbers,
            individualNumbers,
            items,
            p => p.PlotlijnId,
            p => p.Dag,
            p => p.StarttijdId,
            p => p.Order,
            p => p.PaginaId);

        result.Should().Be("2.1");
    }

    [Test]
    public void Plotlijn_member_with_Dag_None_gets_highest_sub_number()
    {
        var plotlijnId = PlotlijnId.Next();
        var scheduled = new TestPage(Dag.Maandag, StarttijdIds.Tijd1030, 1, plotlijnId);
        var unassigned = new TestPage(Dag.None, StarttijdIds.None, 1, plotlijnId);
        var items = new[] { unassigned, scheduled };

        var plotlijnNumbers = new Dictionary<PlotlijnId, int> { [plotlijnId] = 1 };
        var individualNumbers = new Dictionary<PlotboekPaginaId, int>();

        var scheduledResult = PlotboekExportBuilder.GetDisplayNumber(
            plotlijnId,
            scheduled.PaginaId,
            plotlijnNumbers,
            individualNumbers,
            items,
            p => p.PlotlijnId,
            p => p.Dag,
            p => p.StarttijdId,
            p => p.Order,
            p => p.PaginaId);
        var unassignedResult = PlotboekExportBuilder.GetDisplayNumber(
            plotlijnId,
            unassigned.PaginaId,
            plotlijnNumbers,
            individualNumbers,
            items,
            p => p.PlotlijnId,
            p => p.Dag,
            p => p.StarttijdId,
            p => p.Order,
            p => p.PaginaId);

        scheduledResult.Should().Be("1.1");
        unassignedResult.Should().Be("1.2");
    }

    internal sealed record TestPage
    {
        public TestPage(Dag dag, string starttijdId, int order, PlotlijnId plotlijnId = default)
        {
            PaginaId = PlotboekPaginaId.Next();
            Dag = dag;
            StarttijdId = starttijdId;
            Order = order;
            PlotlijnId = plotlijnId;
        }

        public PlotboekPaginaId PaginaId { get; }

        public PlotlijnId PlotlijnId { get; }

        public Dag Dag { get; }

        public string StarttijdId { get; }

        public int Order { get; }
    }
}

