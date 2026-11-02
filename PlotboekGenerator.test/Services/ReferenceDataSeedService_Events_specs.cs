namespace PlotboekGenerator.test.Services;

public sealed class ReferenceDataSeedService_Events_specs
{
    private TestDocumentStore _documentStore;
    private ReferenceDataSeedService _sut;

    [SetUp]
    public void SetUp()
    {
        _documentStore = new TestDocumentStore();
        _sut = new ReferenceDataSeedService(TestReferenceData.Seed, _documentStore, new CapturingLogger<ReferenceDataSeedService>());
    }

    [Test]
    public async Task Seeds_every_event_with_its_days_into_an_empty_store()
    {
        await _sut.SeedAsync();

        _documentStore.GetAll<EventConfig>().Should().BeEquivalentTo(
            TestReferenceData.Seed.Events.Select(e => e with { DagenIngesteld = true }),
            "a new row carries the days it was seeded with");
    }

    [Test]
    public async Task Fills_in_the_days_of_a_row_stored_before_they_existed()
    {
        var seeded = TestReferenceData.Seed.Events.First(e => !e.IsNone && !e.IsEvergreen);
        _documentStore.Seed(seeded with { StartDag = Dag.None, EindDag = Dag.None, DagenIngesteld = false });

        await _sut.SeedAsync();

        _documentStore.Load<EventConfig>(seeded.Id).Should().BeEquivalentTo(new
        {
            seeded.StartDag,
            seeded.EindDag,
            DagenIngesteld = true,
        });
    }

    [Test]
    public async Task Leaves_days_an_admin_set_alone()
    {
        var seeded = TestReferenceData.Seed.Events.First(e => !e.IsNone && !e.IsEvergreen);
        _documentStore.Seed(seeded with { StartDag = Dag.Woensdag, EindDag = Dag.Zaterdag, DagenIngesteld = true });

        await _sut.SeedAsync();

        _documentStore.Load<EventConfig>(seeded.Id).Should().BeEquivalentTo(new { StartDag = Dag.Woensdag, EindDag = Dag.Zaterdag });
    }

    [Test]
    public async Task Marks_a_row_the_seed_does_not_know_without_inventing_days()
    {
        var own = new EventConfig
        {
            Id = "EigenEvent",
            EnumValue = 99,
            DisplayName = "Eigen event",
            Abbreviation = "EE",
            Order = 9,
            IsEvergreen = false,
            IsNone = false,
        };
        _documentStore.Seed(own);

        await _sut.SeedAsync();

        _documentStore.Load<EventConfig>("EigenEvent").Should().BeEquivalentTo(new
        {
            StartDag = Dag.None,
            EindDag = Dag.None,
            DagenIngesteld = true,
        }, "an admin's own event is never touched again, and there is no seed to take days from");
    }
}
