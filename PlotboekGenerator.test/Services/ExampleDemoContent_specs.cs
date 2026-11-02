using Microsoft.Extensions.Logging.Abstractions;
using PlotboekGenerator.Application.DTOs;
using PlotboekGenerator.Application.Ports;
using PlotboekGenerator.infrastructure.Services;
using PlotboekGenerator.test.TestHelpers;

namespace PlotboekGenerator.test.Services;

/// <summary>
/// The example a demo of the generic host shows has to write cleanly against its reference data, in
/// Dutch and in English, and has to show a plot in each of the states it passes through. The English
/// example is the Dutch one translated: the same shape, the same ids.
/// </summary>
public sealed class ExampleDemoContent_specs
{
    private TestDocumentStore _store = null!;
    private InMemoryEventStore _events = null!;
    private bool _referenceDataSeeded;

    [SetUp]
    public void SetUp()
    {
        _store = new TestDocumentStore();
        _events = new InMemoryEventStore();
        _referenceDataSeeded = false;
    }

    [TestCase("nl", "De Nevelkroon")]
    [TestCase("en", "The Mist Crown")]
    public async Task The_example_writes_against_the_example_reference_data(string language, string titel)
    {
        var result = await (await Seeder(language)).SeedAsync();

        result.IsValid.Should().BeTrue(string.Join(", ", result.Messages.Select(m => m.Message)));
        _store.GetAll<Plotboek.ExportData>().Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Titel = titel, EventId = "Voorjaar", IsVolledigKlaar = false });
        _store.GetAll<PlotboekPagina.ExportData>().Should().HaveCount(7);
        _events.StreamCount.Should().Be(8, "one stream for the plotboek and one per pagina");
    }

    [TestCase("nl")]
    [TestCase("en")]
    public async Task The_example_shows_every_state_of_a_plot(string language)
    {
        await (await Seeder(language)).SeedAsync();

        var paginas = _store.GetAll<PlotboekPagina.ExportData>();

        paginas.Count(p => p.IsVolledigKlaar).Should().Be(4, "most of the weekend is written");
        paginas.Should().ContainSingle(p => p.Feedback.Any(f => f.Status == FeedbackStatus.Open), "one plot waits on feedback");
        paginas.Should().ContainSingle(p => p.Feedback.Any(f => f.Status == FeedbackStatus.Afgehandeld), "one plot had feedback signed off");
        paginas.Should().ContainSingle(p => p.IsSpelerGedreven, "one plot is player-driven");
        paginas.Should().OnlyContain(p => p.DomainId != Guid.Empty, "every plot is in a domain");
    }

    [TestCase("nl", "De bode uit Nevelmeer", "Het verzegelde archief", "De Nevelkroon ontwaakt")]
    [TestCase("en", "The messenger from Mistmere", "The sealed archive", "The Mist Crown awakens")]
    public async Task The_plot_line_runs_through_the_crown_plots_in_order(string language, string first, string second, string third)
    {
        await (await Seeder(language)).SeedAsync();

        _store.GetAll<Plotlijn>().Should().ContainSingle()
            .Which.PaginaTitels.Should().Equal(first, second, third);
    }

    [TestCase("nl")]
    [TestCase("en")]
    public async Task The_spelleiders_are_dummies_listed_as_users(string language)
    {
        await (await Seeder(language)).SeedAsync();

        _store.GetAll<PlotboekUser>().Should().HaveCount(5).And.OnlyContain(u => u.IsDummy);
        _store.GetAll<UserRole>().Should().HaveCount(5).And.OnlyContain(r => r.RoleName == UserRoles.User);
    }

    [TestCase("nl")]
    [TestCase("en")]
    public async Task The_domains_are_written_when_there_are_none(string language)
    {
        await (await Seeder(language)).SeedAsync();

        _store.GetAll<Domain>().OrderBy(d => d.Order).Select(d => d.Shorthand)
            .Should().Equal(TestReferenceData.For(language).Domeinen.Select(d => d.Shorthand), "they keep the order of the seed");
    }

    [TestCase("nl")]
    [TestCase("en")]
    public async Task A_demo_that_has_plotboeken_is_left_alone(string language)
    {
        await (await Seeder(language)).SeedAsync();

        var again = await (await Seeder(language)).SeedIfEmptyAsync();

        again.IsValid.Should().BeTrue();
        _store.GetAll<Plotboek.ExportData>().Should().ContainSingle("the example is written once, not on every start");
    }

    [Test]
    public void The_English_example_has_the_shape_of_the_Dutch_one()
    {
        var dutch = Content("nl");
        var english = Content("en");

        english.Spelleiders.Select(s => (s.Id, Domain("en", s.DomainShorthand)))
            .Should().Equal(dutch.Spelleiders.Select(s => (s.Id, Domain("nl", s.DomainShorthand))), "the same spelleiders lead the same domains");
        english.Plotboeken.Select(p => Shape("en", p)).Should().BeEquivalentTo(
            dutch.Plotboeken.Select(p => Shape("nl", p)),
            o => o.WithStrictOrdering(),
            "only what people read is translated");
    }

    [Test]
    public void The_English_example_uses_the_English_domain_shorthands()
    {
        var shorthands = TestReferenceData.English.Domeinen.Select(d => d.Shorthand).ToHashSet(StringComparer.Ordinal);
        var english = Content("en");

        english.Spelleiders.Select(s => s.DomainShorthand)
            .Concat(english.Plotboeken.SelectMany(p => p.Paginas).Select(p => p.DomainShorthand))
            .Should().OnlyContain(shorthand => shorthands.Contains(shorthand));
    }

    /// <summary>What the example is without its words: everything stored data and the states depend on.</summary>
    private static object Shape(string language, DemoPlotboek plotboek)
        => new
        {
            plotboek.EventId,
            plotboek.AangemaaktDoorId,
            plotboek.IsVolledigKlaar,
            Paginas = plotboek.Paginas.Select(p => new
            {
                Domain = Domain(language, p.DomainShorthand),
                InPlotlijn = !string.IsNullOrEmpty(p.Plotlijn),
                Feedback = p.Feedback.Select(f => new { f.GegevenDoorId, HasReactie = !string.IsNullOrEmpty(f.Reactie), f.IsAfgehandeld }).ToList(),
                p.Data.Dag,
                p.Data.StarttijdId,
                p.Data.Order,
                p.Data.VerantwoordelijkeSlId,
                p.Data.ExtraSlIds,
                p.Data.AantalBlokkenId,
                EncounterTypes = new { p.Data.EncounterTypeIds.Major, p.Data.EncounterTypeIds.Minor },
                Locatie = p.Data.LocatieEncounter.Locatie,
                LocatieId = p.Data.LocatieEncounter.LocatieId,
                Npcs = p.Data.AantalNpcs.Rollen?.Select(r => new { r.MinimumAantal, r.MaximumAantal, r.VereistAfstemming }).ToList(),
                AlleNpcs = p.Data.AantalNpcs.AlleNpcs,
                p.Data.Iconen,
                Props = p.Data.PropsEnLoot.Props?.Select(i => new { i.VerantwoordelijkeSlId, i.VerantwoordelijkeSource }).ToList(),
                Loot = p.Data.PropsEnLoot.Loot?.Select(i => new { i.VerantwoordelijkeSlId, i.VerantwoordelijkeSource }).ToList(),
                Debrief = p.Data.Debrief.Vragen?.Count,
                HasInstructie = !string.IsNullOrEmpty(p.Data.InstructiePentagon),
                HasMeerInformatie = !string.IsNullOrEmpty(p.Data.MeerInformatie),
                HasOpmerkingen = !string.IsNullOrEmpty(p.Data.Opmerkingen),
                p.Data.IsFlexibel,
                p.Data.IsSpelerGedreven,
                p.Data.IsVolledigKlaar,
            }).ToList(),
        };

    /// <summary>The place of a domain in its seed, which is what a shorthand means across languages.</summary>
    private static int Domain(string language, string shorthand)
        => TestReferenceData.For(language).Domeinen.Select(d => d.Shorthand).ToList().IndexOf(shorthand);

    private static IDemoContent Content(string language)
        => language == "en" ? new PlotboekGenerator.Host.EnglishExampleDemoContent() : new PlotboekGenerator.Host.ExampleDemoContent();

    /// <summary>
    /// The writer of the example in <paramref name="language"/>, after the first call has seeded that
    /// language's reference data, as a start does before the example is written.
    /// </summary>
    private async Task<DemoContentSeedService> Seeder(string language)
    {
        var seed = TestReferenceData.For(language);
        if (!_referenceDataSeeded)
        {
            await new ReferenceDataSeedService(seed, _store, NullLogger<ReferenceDataSeedService>.Instance).SeedAsync();
            _referenceDataSeeded = true;
        }

        return new (
            Content(language),
            seed,
            _store,
            new PlotboekRepository(_events),
            new PlotboekPaginaRepository(_events),
            NullLogger<DemoContentSeedService>.Instance);
    }
}
