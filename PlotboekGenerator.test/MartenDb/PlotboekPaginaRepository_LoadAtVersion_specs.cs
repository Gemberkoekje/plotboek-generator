using PlotboekGenerator.core.Events;

namespace PlotboekGenerator.test.MartenDb;

public sealed class PlotboekPaginaRepository_LoadAtVersion_specs
{
    private InMemoryEventStore _eventStore;
    private PlotboekPaginaRepository _sut;

    [SetUp]
    public void SetUp()
    {
        _eventStore = new InMemoryEventStore();
        _sut = new PlotboekPaginaRepository(_eventStore);
    }

    [Test]
    public async Task Reconstructs_state_at_requested_version()
    {
        var id = await SeedThreeTitleVersions();

        (await _sut.LoadAtVersionAsync(id, 1)).Value.Snapshot.Titel.Should().Be("V1");
        (await _sut.LoadAtVersionAsync(id, 2)).Value.Snapshot.Titel.Should().Be("V2");
        (await _sut.LoadAtVersionAsync(id, 3)).Value.Snapshot.Titel.Should().Be("V3");
    }

    [Test]
    public async Task Returns_current_version_as_total_event_count()
    {
        var id = await SeedThreeTitleVersions();

        var result = await _sut.LoadAtVersionAsync(id, 1);

        result.IsValid.Should().BeTrue();
        result.Value.CurrentVersion.Should().Be(3);
    }

    [Test]
    public async Task Clamps_version_above_current_to_latest()
    {
        var id = await SeedThreeTitleVersions();

        var result = await _sut.LoadAtVersionAsync(id, 99);

        result.Value.Snapshot.Titel.Should().Be("V3");
        result.Value.CurrentVersion.Should().Be(3);
        result.Value.TargetVersion.Should().Be(3, "the requested version is clamped to the latest");
    }

    [Test]
    public async Task Clamps_version_below_one_to_first()
    {
        var id = await SeedThreeTitleVersions();

        var result = await _sut.LoadAtVersionAsync(id, 0);

        result.Value.Snapshot.Titel.Should().Be("V1");
        result.Value.TargetVersion.Should().Be(1);
    }

    [Test]
    public async Task Returns_invalid_when_stream_is_empty()
    {
        var id = PlotboekPaginaId.Next();

        var result = await _sut.LoadAtVersionAsync(id, 1);

        result.IsValid.Should().BeFalse();
    }

    [Test]
    public async Task Returns_invalid_when_id_is_empty()
    {
        var result = await _sut.LoadAtVersionAsync(PlotboekPaginaId.Empty, 1);

        result.IsValid.Should().BeFalse();
    }

    private async Task<PlotboekPaginaId> SeedThreeTitleVersions()
    {
        var id = PlotboekPaginaId.Next();
        var result = await _eventStore.SaveEventsAsync((Uuid)id, 0, new object[]
        {
            new PlotboekPaginaTitelAangepast { Titel = "V1", Who = "user-1" },
            new PlotboekPaginaTitelAangepast { Titel = "V2", Who = "user-1" },
            new PlotboekPaginaTitelAangepast { Titel = "V3", Who = "user-1" },
        });
        result.IsValid.Should().BeTrue("seed events should be saved successfully");
        return id;
    }
}
