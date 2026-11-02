using PlotboekGenerator.infrastructure;

namespace PlotboekGenerator.test.TestHelpers;

/// <summary>
/// Pins the insert-only contract that <see cref="IDocumentSession.Insert{T}"/> promises and the admin
/// create paths rely on. In production the guarantee comes from the document's primary key; the
/// in-memory fake has to reproduce it — including the timing, because Marten only discovers the
/// collision at commit time and callers put their try/catch around SaveChanges accordingly.
/// </summary>
public sealed class TestDocumentStore_Insert_specs
{
    private TestDocumentStore _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new TestDocumentStore();

    [Test]
    public async Task Insert_stores_a_document_that_is_not_there_yet()
    {
        using var session = _sut.OpenSession();
        session.Insert(Locatie("Zuidveld"));

        await session.SaveChangesAsync();

        Stored().DisplayName.Should().Be("Zuidveld");
    }

    [Test]
    public async Task Insert_refuses_to_overwrite_an_existing_document()
    {
        _sut.Seed(Locatie("Van de eerste admin"));

        using var session = _sut.OpenSession();
        session.Insert(Locatie("Van de tweede admin"));

        var save = async () => await session.SaveChangesAsync();

        await save.Should().ThrowAsync<DocumentAlreadyExistsException>(
            "two admins creating the same name must not silently overwrite each other");
        Stored().DisplayName.Should().Be("Van de eerste admin");
    }

    [Test]
    public void Insert_does_not_fail_before_the_save()
    {
        _sut.Seed(Locatie("Van de eerste admin"));

        using var session = _sut.OpenSession();

        // Staging alone must not throw: Marten only hits the primary key when it commits, and the
        // admin handlers put their try/catch around SaveChangesAsync accordingly.
        var stage = () => session.Insert(Locatie("Van de tweede admin"));

        stage.Should().NotThrow();
    }

    [Test]
    public async Task Store_still_overwrites_because_it_is_an_upsert()
    {
        _sut.Seed(Locatie("Van de eerste admin"));

        using var session = _sut.OpenSession();
        session.Store(Locatie("Van de tweede admin"));
        await session.SaveChangesAsync();

        Stored().DisplayName.Should().Be(
            "Van de tweede admin",
            "Store is the upsert; only Insert refuses a collision");
    }

    private static QuestLocatieConfig Locatie(string displayName) => new ()
    {
        Id = "Zuidveld",
        DisplayName = displayName,
        CategorieId = "Plekken",
        IsActief = true,
        Order = 1,
    };

    private QuestLocatieConfig Stored() => _sut.Load<QuestLocatieConfig>("Zuidveld")!;
}
