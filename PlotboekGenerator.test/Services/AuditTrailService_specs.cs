using Qowaiv.Validation.Abstractions;
using PlotboekGenerator.core.Events;
using PlotboekGenerator.test.TestHelpers;

namespace PlotboekGenerator.test.Services;

public sealed class AuditTrailService_specs
{
    private InMemoryEventStore _eventStore;
    private TestDocumentStore _documentStore;
    private AuditTrailService _sut;

    [SetUp]
    public void SetUp()
    {
        _eventStore = new InMemoryEventStore();
        _documentStore = new TestDocumentStore();
        _sut = new AuditTrailService(_documentStore, _eventStore);
    }

    [Test]
    public async Task Returns_empty_list_when_no_events_exist()
    {
        var aggregateId = Uuid.NewUuid();

        var result = await _sut.GetAuditTrailAsync(aggregateId);

        result.Should().BeEmpty();
    }

    [Test]
    public async Task Returns_audit_trail_entries_for_stored_events()
    {
        var aggregateId = Uuid.NewUuid();
        var userId = Guid.NewGuid().ToString();
        var when = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        SeedUser(userId, "TestUser");
        await SaveEvents(
            aggregateId,
            0,
            new PlotboekTitelAangepast { Who = userId, When = when, Titel = "Nieuw" });

        var result = await _sut.GetAuditTrailAsync(aggregateId);

        result.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                Who = "TestUser",
                When = when,
                What = "<strong>Titel aangepast:</strong> Nieuw",
            });
    }

    [Test]
    public async Task Resolves_username_from_document_store()
    {
        var aggregateId = Uuid.NewUuid();
        var userId = Guid.NewGuid().ToString();

        SeedUser(userId, "Alice");
        await SaveEvents(
            aggregateId,
            0,
            new PlotboekTitelAangepast { Who = userId, When = DateTime.UtcNow, Titel = "X" });

        var result = await _sut.GetAuditTrailAsync(aggregateId);

        result.Should().ContainSingle()
            .Which.Who.Should().Be("Alice");
    }

    [Test]
    public async Task Falls_back_to_user_id_when_user_not_found()
    {
        var aggregateId = Uuid.NewUuid();
        var unknownUserId = Guid.NewGuid().ToString();

        await SaveEvents(
            aggregateId,
            0,
            new PlotboekTitelAangepast { Who = unknownUserId, When = DateTime.UtcNow, Titel = "X" });

        var result = await _sut.GetAuditTrailAsync(aggregateId);

        result.Should().ContainSingle()
            .Which.Who.Should().Be(unknownUserId);
    }

    [Test]
    public async Task Falls_back_to_user_id_when_username_is_null()
    {
        var aggregateId = Uuid.NewUuid();
        var userId = Guid.NewGuid().ToString();

        SeedUserWithoutUserName(userId);
        await SaveEvents(
            aggregateId,
            0,
            new PlotboekTitelAangepast { Who = userId, When = DateTime.UtcNow, Titel = "X" });

        var result = await _sut.GetAuditTrailAsync(aggregateId);

        result.Should().ContainSingle()
            .Which.Who.Should().Be(userId);
    }

    [Test]
    public async Task Returns_entries_ordered_by_most_recent_first()
    {
        var aggregateId = Uuid.NewUuid();
        var userId = Guid.NewGuid().ToString();
        var earlier = new DateTime(2025, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var later = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        SeedUser(userId, "Bob");
        await SaveEvents(
            aggregateId,
            0,
            new PlotboekTitelAangepast { Who = userId, When = earlier, Titel = "Eerste" },
            new PlotboekTitelAangepast { Who = userId, When = later, Titel = "Tweede" });

        var result = await _sut.GetAuditTrailAsync(aggregateId);

        result.Should().HaveCount(2);
        result[0].When.Should().Be(later);
        result[1].When.Should().Be(earlier);
    }

    [Test]
    public async Task Ignores_non_audit_trail_events_in_stream()
    {
        var aggregateId = Uuid.NewUuid();
        var userId = Guid.NewGuid().ToString();

        SeedUser(userId, "Charlie");
        await SaveEvents(
            aggregateId,
            0,
            new NonAuditEvent(),
            new PlotboekTitelAangepast { Who = userId, When = DateTime.UtcNow, Titel = "Titel" });

        var result = await _sut.GetAuditTrailAsync(aggregateId);

        result.Should().ContainSingle()
            .Which.What.Should().Contain("Titel aangepast");
    }

    [Test]
    public async Task Resolves_multiple_distinct_users()
    {
        var aggregateId = Uuid.NewUuid();
        var userId1 = Guid.NewGuid().ToString();
        var userId2 = Guid.NewGuid().ToString();

        SeedUser(userId1, "Alice");
        SeedUser(userId2, "Bob");
        await SaveEvents(
            aggregateId,
            0,
            new PlotboekTitelAangepast { Who = userId1, When = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), Titel = "A" },
            new PlotboekTitelAangepast { Who = userId2, When = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc), Titel = "B" });

        var result = await _sut.GetAuditTrailAsync(aggregateId);

        result.Should().HaveCount(2);
        result.Select(e => e.Who).Should().BeEquivalentTo("Bob", "Alice");
    }

    [Test]
    public async Task Includes_Details_from_event()
    {
        var aggregateId = Uuid.NewUuid();
        var userId = Guid.NewGuid().ToString();

        SeedUser(userId, "Dave");
        await SaveEvents(
            aggregateId,
            0,
            new AuditEventWithDetails { Who = userId, When = DateTime.UtcNow });

        var result = await _sut.GetAuditTrailAsync(aggregateId);

        result.Should().ContainSingle()
            .Which.Details.Should().Be("Extra info");
    }

    [Test]
    public async Task Details_is_null_when_event_has_no_details()
    {
        var aggregateId = Uuid.NewUuid();
        var userId = Guid.NewGuid().ToString();

        SeedUser(userId, "Eve");
        await SaveEvents(
            aggregateId,
            0,
            new PlotboekTitelAangepast { Who = userId, When = DateTime.UtcNow, Titel = "T" });

        var result = await _sut.GetAuditTrailAsync(aggregateId);

        result.Should().ContainSingle()
            .Which.Details.Should().BeNull();
    }

    [Test]
    public async Task Includes_raw_debug_details_for_rich_text_audit_events()
    {
        var aggregateId = Uuid.NewUuid();
        var userId = Guid.NewGuid().ToString();
        const string richText = "&nbsp;<br><h1>bla</h1><div>&nbsp;</div>";

        SeedUser(userId, "Frank");
        await SaveEvents(
            aggregateId,
            0,
            new PlotboekPaginaMeerInformatieAangepast
            {
                Who = userId,
                When = DateTime.UtcNow,
                MeerInformatie = richText,
            });

        var result = await _sut.GetAuditTrailAsync(aggregateId);

        result.Should().ContainSingle();
        result[0].Details.Should().Be(richText);
        result[0].DebugDetails.Should().Be(richText);
    }

    [Test]
    public async Task Assigns_stream_version_to_each_entry_independent_of_sort_order()
    {
        var aggregateId = Uuid.NewUuid();
        var userId = Guid.NewGuid().ToString();
        var earlier = new DateTime(2025, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var later = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        SeedUser(userId, "Bob");
        await SaveEvents(
            aggregateId,
            0,
            new PlotboekTitelAangepast { Who = userId, When = earlier, Titel = "Eerste" },
            new PlotboekTitelAangepast { Who = userId, When = later, Titel = "Tweede" });

        var result = await _sut.GetAuditTrailAsync(aggregateId);

        // Entries are sorted by When descending, but Version must reflect the 1-based stream position.
        result[0].When.Should().Be(later);
        result[0].Version.Should().Be(2);
        result[1].When.Should().Be(earlier);
        result[1].Version.Should().Be(1);
    }

    [Test]
    public async Task Version_counts_non_audit_events_in_the_stream()
    {
        var aggregateId = Uuid.NewUuid();
        var userId = Guid.NewGuid().ToString();

        SeedUser(userId, "Charlie");
        await SaveEvents(
            aggregateId,
            0,
            new NonAuditEvent(),
            new PlotboekTitelAangepast { Who = userId, When = DateTime.UtcNow, Titel = "Titel" });

        var result = await _sut.GetAuditTrailAsync(aggregateId);

        // The audit entry is the 2nd event in the stream, so restoring to it must target version 2.
        result.Should().ContainSingle()
            .Which.Version.Should().Be(2);
    }

    private void SeedUser(string id, string userName)
    {
        _documentStore.Seed(new PlotboekUser
        {
            Id = id,
            UserName = userName,
            Lidnummer = "TEST",
            FirstName = "Test",
            LastName = "User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDummy = false,
            PrimaryDomainId = Guid.Empty,
        });
    }

    private void SeedUserWithoutUserName(string id)
    {
        _documentStore.Seed(new PlotboekUser
        {
            Id = id,
            Lidnummer = "TEST",
            FirstName = "Test",
            LastName = "User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDummy = false,
            PrimaryDomainId = Guid.Empty,
        });
    }

    private async Task SaveEvents(Uuid aggregateId, long expectedVersion, params object[] events)
    {
        var result = await _eventStore.SaveEventsAsync(aggregateId, expectedVersion, events);
        result.IsValid.Should().BeTrue("events should be saved successfully");
    }

    /// <summary>A non-audit event to verify filtering.</summary>
    private sealed record NonAuditEvent;

    /// <summary>An audit trail event that overrides <see cref="AuditTrailEvent.Details"/>.</summary>
    private sealed record AuditEventWithDetails : AuditTrailEvent
    {
        public override string What => "Something happened";

        public override string Details => "Extra info";
    }
}
