namespace PlotboekGenerator.test.Services;

/// <summary>
/// The example LARP a fresh deployment (and the demo) starts from has to hang together.
/// </summary>
public sealed class ExampleReferenceDataSeed_specs
{
    private static readonly PlotboekGenerator.Application.Ports.IReferenceDataSeed Seed = TestReferenceData.Seed;

    [Test]
    public void Every_event_with_days_starts_before_it_ends()
        => Seed.Events.Where(e => !e.IsNone && !e.IsEvergreen)
            .Should().OnlyContain(e => e.StartDag != Dag.None && e.StartDag <= e.EindDag);

    [Test]
    public void The_event_cycle_points_at_events_that_exist()
    {
        var ids = Seed.Events.Select(e => e.Id).ToHashSet(StringComparer.Ordinal);

        Seed.Events.Where(e => !string.IsNullOrEmpty(e.NextEventId))
            .Should().OnlyContain(e => ids.Contains(e.NextEventId));
    }

    [Test]
    public void Every_quest_location_belongs_to_a_category_that_exists()
    {
        var categories = Seed.QuestLocatieCategorieen.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);

        Seed.QuestLocaties.Should().OnlyContain(l => categories.Contains(l.CategorieId));
    }

    [Test]
    public void Every_slot_with_a_block_window_is_available_on_that_day_type()
        => Seed.Starttijden.Where(s => !s.IsNone).Should().OnlyContain(s =>
            (string.IsNullOrEmpty(s.BlokVensterEersteDag) || s.IsAvailableOnEersteDag)
            && (string.IsNullOrEmpty(s.BlokVensterMiddelDagen) || s.IsAvailableOnMiddelDagen)
            && (string.IsNullOrEmpty(s.BlokVensterLaatsteDag) || s.IsAvailableOnLaatsteDag));

    [Test]
    public void Ids_are_unique_per_kind()
    {
        Seed.Starttijden.Select(s => s.Id).Should().OnlyHaveUniqueItems();
        Seed.AantalBlokken.Select(s => s.Id).Should().OnlyHaveUniqueItems();
        Seed.SoortEncounters.Select(s => s.Id).Should().OnlyHaveUniqueItems();
        Seed.Events.Select(s => s.Id).Should().OnlyHaveUniqueItems();
        Seed.QuestLocatieCategorieen.Select(s => s.Id).Should().OnlyHaveUniqueItems();
        Seed.QuestLocaties.Select(s => s.Id).Should().OnlyHaveUniqueItems();
    }
}
