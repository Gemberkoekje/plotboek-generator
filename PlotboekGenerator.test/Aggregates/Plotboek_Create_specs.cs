namespace PlotboekGenerator.test.Aggregates;

public sealed class Plotboek_Create_specs
{
    private static PlotboekUser TestUser => new ()
    {
        Id = "user-1",
        Lidnummer = "12345",
        FirstName = "Test",
        LastName = "User",
        UserName = "testuser",
        Email = "test@example.test",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsDummy = false,
        PrimaryDomainId = Guid.Empty,
    };

    [Test]
    public void Creates_plotboek_with_jaar_and_event()
    {
        var result = Plotboek.Create(Year.Create(2025), EventIds.Voorjaar, Dag.Vrijdag, Dag.Zondag, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.Jaar.Should().Be(Year.Create(2025));
        result.Value.EventId.Should().Be(EventIds.Voorjaar);
    }

    [Test]
    public void Starts_and_ends_on_the_days_it_is_given()
    {
        var result = Plotboek.Create(Year.Create(2025), EventIds.Zomer, Dag.Donderdag, Dag.Zondag, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.StartDag.Should().Be(Dag.Donderdag, "the days come from the event's config, not from its name");
        result.Value.EindDag.Should().Be(Dag.Zondag);
    }

    [Test]
    public void An_event_without_days_has_no_days()
    {
        var result = Plotboek.Create(Year.Create(2025), EventIds.Evergreen, Dag.None, Dag.None, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.StartDag.Should().Be(Dag.None);
        result.Value.EindDag.Should().Be(Dag.None);
    }

    [Test]
    public void Creates_deterministic_id()
    {
        var result1 = Plotboek.Create(Year.Create(2025), EventIds.Voorjaar, Dag.Vrijdag, Dag.Zondag, TestUser);
        var result2 = Plotboek.Create(Year.Create(2025), EventIds.Voorjaar, Dag.Vrijdag, Dag.Zondag, TestUser);

        result1.Value.Id.Should().Be(result2.Value.Id);
    }

    [Test]
    public void Different_events_produce_different_ids()
    {
        var result1 = Plotboek.Create(Year.Create(2025), EventIds.Voorjaar, Dag.Vrijdag, Dag.Zondag, TestUser);
        var result2 = Plotboek.Create(Year.Create(2025), EventIds.Najaar, Dag.Vrijdag, Dag.Zondag, TestUser);

        result1.Value.Id.Should().NotBe(result2.Value.Id);
    }
}
