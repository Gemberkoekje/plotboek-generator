namespace PlotboekGenerator.test.Services;

public sealed class BuildSlotNumbers_specs
{
    [Test]
    public void Empty_list_returns_empty_dictionaries()
    {
        var items = Array.Empty<TestPage>();

        var (plotlijnNumbers, individualNumbers) = PlotboekExportBuilder.BuildSlotNumbers(
            items, p => p.PlotlijnId, p => p.Dag, p => p.StarttijdId, p => p.Order, p => p.PaginaId);

        plotlijnNumbers.Should().BeEmpty();
        individualNumbers.Should().BeEmpty();
    }

    [Test]
    public void Individual_pages_without_plotlijn_get_sequential_numbers()
    {
        var p1 = new TestPage(Dag.Maandag, StarttijdIds.Tijd1030, 1);
        var p2 = new TestPage(Dag.Maandag, StarttijdIds.Tijd1230, 1);
        var p3 = new TestPage(Dag.Dinsdag, StarttijdIds.Tijd1030, 1);

        var (plotlijnNumbers, individualNumbers) = PlotboekExportBuilder.BuildSlotNumbers(
            [p1, p2, p3], p => p.PlotlijnId, p => p.Dag, p => p.StarttijdId, p => p.Order, p => p.PaginaId);

        plotlijnNumbers.Should().BeEmpty();
        individualNumbers.Should().HaveCount(3);
        individualNumbers[p1.PaginaId].Should().Be(1);
        individualNumbers[p2.PaginaId].Should().Be(2);
        individualNumbers[p3.PaginaId].Should().Be(3);
    }

    [Test]
    public void Plotlijn_groups_get_single_sequential_number()
    {
        var plotlijnId = PlotlijnId.Next();
        var p1 = new TestPage(Dag.Maandag, StarttijdIds.Tijd1030, 1, plotlijnId);
        var p2 = new TestPage(Dag.Maandag, StarttijdIds.Tijd1230, 1, plotlijnId);

        var (plotlijnNumbers, individualNumbers) = PlotboekExportBuilder.BuildSlotNumbers(
            [p1, p2], p => p.PlotlijnId, p => p.Dag, p => p.StarttijdId, p => p.Order, p => p.PaginaId);

        plotlijnNumbers.Should().ContainSingle();
        plotlijnNumbers[plotlijnId].Should().Be(1);
        individualNumbers.Should().BeEmpty();
    }

    [Test]
    public void Mixed_plotlijn_and_individual_pages_are_numbered_by_earliest_occurrence()
    {
        var plotlijnId = PlotlijnId.Next();
        var pIndividual = new TestPage(Dag.Maandag, StarttijdIds.Tijd1030, 1);
        var pPlotlijn1 = new TestPage(Dag.Maandag, StarttijdIds.Tijd1230, 1, plotlijnId);
        var pPlotlijn2 = new TestPage(Dag.Dinsdag, StarttijdIds.Tijd1030, 1, plotlijnId);

        var (plotlijnNumbers, individualNumbers) = PlotboekExportBuilder.BuildSlotNumbers(
            [pIndividual, pPlotlijn1, pPlotlijn2], p => p.PlotlijnId, p => p.Dag, p => p.StarttijdId, p => p.Order, p => p.PaginaId);

        individualNumbers[pIndividual.PaginaId].Should().Be(1);
        plotlijnNumbers[plotlijnId].Should().Be(2);
    }

    [Test]
    public void Multiple_plotlijnen_are_sorted_by_their_earliest_page()
    {
        var plotlijn1 = PlotlijnId.Next();
        var plotlijn2 = PlotlijnId.Next();
        var p1a = new TestPage(Dag.Dinsdag, StarttijdIds.Tijd1030, 1, plotlijn1);
        var p2a = new TestPage(Dag.Maandag, StarttijdIds.Tijd1030, 1, plotlijn2);

        var (plotlijnNumbers, _) = PlotboekExportBuilder.BuildSlotNumbers(
            [p1a, p2a], p => p.PlotlijnId, p => p.Dag, p => p.StarttijdId, p => p.Order, p => p.PaginaId);

        plotlijnNumbers[plotlijn2].Should().BeLessThan(plotlijnNumbers[plotlijn1]);
    }

    [Test]
    public void Pages_with_same_dag_are_sorted_by_starttijd()
    {
        var p1 = new TestPage(Dag.Maandag, StarttijdIds.Tijd1430, 1);
        var p2 = new TestPage(Dag.Maandag, StarttijdIds.Tijd1030, 1);

        var (_, individualNumbers) = PlotboekExportBuilder.BuildSlotNumbers(
            [p1, p2], p => p.PlotlijnId, p => p.Dag, p => p.StarttijdId, p => p.Order, p => p.PaginaId);

        individualNumbers[p2.PaginaId].Should().Be(1);
        individualNumbers[p1.PaginaId].Should().Be(2);
    }

    [Test]
    public void Pages_with_same_dag_and_starttijd_are_sorted_by_order()
    {
        var p1 = new TestPage(Dag.Maandag, StarttijdIds.Tijd1030, 3);
        var p2 = new TestPage(Dag.Maandag, StarttijdIds.Tijd1030, 1);

        var (_, individualNumbers) = PlotboekExportBuilder.BuildSlotNumbers(
            [p1, p2], p => p.PlotlijnId, p => p.Dag, p => p.StarttijdId, p => p.Order, p => p.PaginaId);

        individualNumbers[p2.PaginaId].Should().Be(1);
        individualNumbers[p1.PaginaId].Should().Be(2);
    }

    [Test]
    public void Plotlijn_earliest_is_determined_by_dag_then_starttijd_then_order()
    {
        var plotlijnId = PlotlijnId.Next();
        var late = new TestPage(Dag.Dinsdag, StarttijdIds.Tijd2000, 5, plotlijnId);
        var early = new TestPage(Dag.Maandag, StarttijdIds.Tijd1030, 1, plotlijnId);
        var individual = new TestPage(Dag.Maandag, StarttijdIds.Tijd1430, 1);

        var (plotlijnNumbers, individualNumbers) = PlotboekExportBuilder.BuildSlotNumbers(
            [late, early, individual], p => p.PlotlijnId, p => p.Dag, p => p.StarttijdId, p => p.Order, p => p.PaginaId);

        plotlijnNumbers[plotlijnId].Should().Be(1);
        individualNumbers[individual.PaginaId].Should().Be(2);
    }

    [Test]
    public void Pages_with_Dag_None_get_highest_numbers()
    {
        var scheduled = new TestPage(Dag.Maandag, StarttijdIds.Tijd1030, 1);
        var unassigned = new TestPage(Dag.None, StarttijdIds.None, 1);

        var (_, individualNumbers) = PlotboekExportBuilder.BuildSlotNumbers(
            [unassigned, scheduled], p => p.PlotlijnId, p => p.Dag, p => p.StarttijdId, p => p.Order, p => p.PaginaId);

        individualNumbers[scheduled.PaginaId].Should().Be(1);
        individualNumbers[unassigned.PaginaId].Should().Be(2);
    }

    [Test]
    public void Pages_with_Starttijd_None_sort_after_assigned_starttijd_on_same_dag()
    {
        var assigned = new TestPage(Dag.Maandag, StarttijdIds.Tijd1030, 1);
        var noStarttijd = new TestPage(Dag.Maandag, StarttijdIds.None, 1);

        var (_, individualNumbers) = PlotboekExportBuilder.BuildSlotNumbers(
            [noStarttijd, assigned], p => p.PlotlijnId, p => p.Dag, p => p.StarttijdId, p => p.Order, p => p.PaginaId);

        individualNumbers[assigned.PaginaId].Should().Be(1);
        individualNumbers[noStarttijd.PaginaId].Should().Be(2);
    }

    [Test]
    public void Plotlijn_with_only_unassigned_pages_gets_highest_number()
    {
        var plotlijnId = PlotlijnId.Next();
        var scheduledIndividual = new TestPage(Dag.Maandag, StarttijdIds.Tijd1030, 1);
        var unassignedPlotlijn = new TestPage(Dag.None, StarttijdIds.None, 1, plotlijnId);

        var (plotlijnNumbers, individualNumbers) = PlotboekExportBuilder.BuildSlotNumbers(
            [unassignedPlotlijn, scheduledIndividual], p => p.PlotlijnId, p => p.Dag, p => p.StarttijdId, p => p.Order, p => p.PaginaId);

        individualNumbers[scheduledIndividual.PaginaId].Should().Be(1);
        plotlijnNumbers[plotlijnId].Should().Be(2);
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

