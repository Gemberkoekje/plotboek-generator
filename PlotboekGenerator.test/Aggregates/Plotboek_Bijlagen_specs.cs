using PlotboekGenerator.core.Events;

namespace PlotboekGenerator.test.Aggregates;

/// <summary>
/// Guards the book-level bijlagen collection on <see cref="Plotboek"/>: setting, the no-change guard,
/// and normalization (dropping title-less rows, renumbering Order).
/// </summary>
public sealed class Plotboek_Bijlagen_specs
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

    private static Plotboek CreatePlotboek()
    {
        var plotboek = Plotboek.Create(Year.Create(2025), EventIds.Voorjaar, Dag.Vrijdag, Dag.Zondag, TestUser).Value;
        plotboek.MarkAllAsCommitted();
        return plotboek;
    }

    [Test]
    public void Aanpassen_sets_the_bijlagen()
    {
        var plotboek = CreatePlotboek();
        var id = BijlageId.Next();

        var result = plotboek.Aanpassen(new Plotboek.BijlagenData
        {
            Bijlagen = [BijlageData.Create(id, "Prijslijst", "<p>Brazen Bull</p>", 0)],
        }, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.Bijlagen.Should().ContainSingle();
        result.Value.Bijlagen[0].Id.Should().Be(id);
        result.Value.Bijlagen[0].Titel.Should().Be("Prijslijst");
    }

    [Test]
    public void Aanpassen_drops_rows_without_a_title()
    {
        var plotboek = CreatePlotboek();

        var result = plotboek.Aanpassen(new Plotboek.BijlagenData
        {
            Bijlagen =
            [
                BijlageData.Create(BijlageId.Next(), "   ", "<p>geen titel</p>", 0),
                BijlageData.Create(BijlageId.Next(), "Statblock", "<p>AC 15</p>", 1),
            ],
        }, TestUser).Value;

        result.Bijlagen.Should().ContainSingle();
        result.Bijlagen[0].Titel.Should().Be("Statblock");
    }

    [Test]
    public void Aanpassen_renumbers_order_to_list_position()
    {
        var plotboek = CreatePlotboek();

        var result = plotboek.Aanpassen(new Plotboek.BijlagenData
        {
            Bijlagen =
            [
                BijlageData.Create(BijlageId.Next(), "Eerste", "<p>a</p>", 99),
                BijlageData.Create(BijlageId.Next(), "Tweede", "<p>b</p>", 42),
            ],
        }, TestUser).Value;

        result.Bijlagen[0].Order.Should().Be(0);
        result.Bijlagen[1].Order.Should().Be(1);
    }

    [Test]
    public void Aanpassen_emits_a_single_bijlagen_event()
    {
        var plotboek = CreatePlotboek();

        var result = plotboek.Aanpassen(new Plotboek.BijlagenData
        {
            Bijlagen = [BijlageData.Create(BijlageId.Next(), "Prijslijst", "<p>x</p>", 0)],
        }, TestUser);

        result.Value.Buffer.Uncommitted.Should().ContainSingle()
            .Which.Should().BeOfType<PlotboekBijlagenAangepast>();
    }

    [Test]
    public void Aanpassen_is_noop_when_unchanged()
    {
        var plotboek = CreatePlotboek();
        var data = new Plotboek.BijlagenData
        {
            Bijlagen = [BijlageData.Create(BijlageId.Next(), "Prijslijst", "<p>x</p>", 0)],
        };
        plotboek = plotboek.Aanpassen(data, TestUser).Value;
        plotboek.MarkAllAsCommitted();

        var result = plotboek.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.Buffer.Uncommitted.Should().BeEmpty();
    }
}
