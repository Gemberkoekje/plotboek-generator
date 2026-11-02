namespace PlotboekGenerator.test.Services;

/// <summary>
/// The example LARP a fresh deployment (and the demo) starts from has to hang together, in Dutch and
/// in English, and both languages have to describe the same LARP: the same ids, the same days.
/// </summary>
public sealed class ExampleReferenceDataSeed_specs
{
    [TestCase("nl")]
    [TestCase("en")]
    public void Every_event_with_days_starts_before_it_ends(string language)
        => TestReferenceData.For(language).Events.Where(e => !e.IsNone && !e.IsEvergreen)
            .Should().OnlyContain(e => e.StartDag != Dag.None && e.StartDag <= e.EindDag);

    [TestCase("nl")]
    [TestCase("en")]
    public void The_event_cycle_points_at_events_that_exist(string language)
    {
        var seed = TestReferenceData.For(language);
        var ids = seed.Events.Select(e => e.Id).ToHashSet(StringComparer.Ordinal);

        seed.Events.Where(e => !string.IsNullOrEmpty(e.NextEventId))
            .Should().OnlyContain(e => ids.Contains(e.NextEventId));
    }

    [TestCase("nl")]
    [TestCase("en")]
    public void Every_quest_location_belongs_to_a_category_that_exists(string language)
    {
        var seed = TestReferenceData.For(language);
        var categories = seed.QuestLocatieCategorieen.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);

        seed.QuestLocaties.Should().OnlyContain(l => categories.Contains(l.CategorieId));
    }

    [TestCase("nl")]
    [TestCase("en")]
    public void Every_slot_with_a_block_window_is_available_on_that_day_type(string language)
        => TestReferenceData.For(language).Starttijden.Where(s => !s.IsNone).Should().OnlyContain(s =>
            (string.IsNullOrEmpty(s.BlokVensterEersteDag) || s.IsAvailableOnEersteDag)
            && (string.IsNullOrEmpty(s.BlokVensterMiddelDagen) || s.IsAvailableOnMiddelDagen)
            && (string.IsNullOrEmpty(s.BlokVensterLaatsteDag) || s.IsAvailableOnLaatsteDag));

    [TestCase("nl")]
    [TestCase("en")]
    public void Ids_are_unique_per_kind(string language)
    {
        var seed = TestReferenceData.For(language);

        seed.Starttijden.Select(s => s.Id).Should().OnlyHaveUniqueItems();
        seed.AantalBlokken.Select(s => s.Id).Should().OnlyHaveUniqueItems();
        seed.SoortEncounters.Select(s => s.Id).Should().OnlyHaveUniqueItems();
        seed.Events.Select(s => s.Id).Should().OnlyHaveUniqueItems();
        seed.QuestLocatieCategorieen.Select(s => s.Id).Should().OnlyHaveUniqueItems();
        seed.QuestLocaties.Select(s => s.Id).Should().OnlyHaveUniqueItems();
        seed.Domeinen.Select(d => d.Shorthand).Should().OnlyHaveUniqueItems("a plot finds its domain by shorthand");
    }

    [TestCase("nl")]
    [TestCase("en")]
    public void Every_list_the_admin_page_starts_from_is_filled(string language)
    {
        var seed = TestReferenceData.For(language);

        seed.Meetings.Should().NotBeEmpty();
        seed.KarakterNamen.Should().NotBeEmpty().And.OnlyHaveUniqueItems();
        seed.WereldNamen.Should().NotBeEmpty().And.OnlyHaveUniqueItems();
    }

    [Test]
    public void Both_languages_have_exactly_the_same_ids_per_kind()
    {
        var dutch = TestReferenceData.Seed;
        var english = TestReferenceData.English;

        english.Starttijden.Select(s => s.Id).Should().Equal(dutch.Starttijden.Select(s => s.Id), "stored plots refer to a start time by id");
        english.AantalBlokken.Select(s => s.Id).Should().Equal(dutch.AantalBlokken.Select(s => s.Id), "stored plots refer to a length by id");
        english.SoortEncounters.Select(s => s.Id).Should().Equal(dutch.SoortEncounters.Select(s => s.Id), "stored plots refer to an encounter type by id");
        english.Events.Select(s => s.Id).Should().Equal(dutch.Events.Select(s => s.Id), "a plotboek refers to its event by id");
        english.QuestLocatieCategorieen.Select(s => s.Id).Should().Equal(dutch.QuestLocatieCategorieen.Select(s => s.Id), "a quest location refers to its category by id");
        english.QuestLocaties.Select(s => s.Id).Should().Equal(dutch.QuestLocaties.Select(s => s.Id), "stored plots refer to a quest location by id");
    }

    [Test]
    public void Both_languages_describe_the_same_LARP()
    {
        var dutch = TestReferenceData.Seed;
        var english = TestReferenceData.English;

        english.Starttijden.Should().BeEquivalentTo(dutch.Starttijden, "a time slot reads the same in every language");
        english.Events.Should().BeEquivalentTo(dutch.Events, o => o.Excluding(e => e.DisplayName).Excluding(e => e.Abbreviation), "the events have the same days and cycle");
        english.AantalBlokken.Should().BeEquivalentTo(dutch.AantalBlokken, o => o.Excluding(b => b.DisplayName), "a length is as long in every language");
        english.SoortEncounters.Should().BeEquivalentTo(dutch.SoortEncounters, o => o.Excluding(s => s.DisplayName), "only the names are translated");
        english.QuestLocatieCategorieen.Should().BeEquivalentTo(dutch.QuestLocatieCategorieen, o => o.Excluding(c => c.DisplayName).Excluding(c => c.Notitie), "only the names and notes are translated");
        english.QuestLocaties.Should().BeEquivalentTo(dutch.QuestLocaties, o => o.Excluding(l => l.DisplayName).Excluding(l => l.Notitie), "the same places clash");
        english.Domeinen.Should().HaveSameCount(dutch.Domeinen, "every domain has an English name");
        english.Meetings.Select(m => (m.DagType, m.Tijd)).Should().Equal(dutch.Meetings.Select(m => (m.DagType, m.Tijd)), "the day has the same fixed moments");
        english.KarakterNamen.Should().HaveSameCount(dutch.KarakterNamen);
        english.WereldNamen.Should().HaveSameCount(dutch.WereldNamen);
    }

    [Test]
    public void The_English_example_has_its_own_domain_shorthands()
        => TestReferenceData.English.Domeinen.Should().Equal(
            [("C", "Court"), ("G", "Guilds"), ("T", "Temple"), ("W", "Wilderness"), ("O", "Other")],
            "a shorthand is the first letter of the domain's name");
}
