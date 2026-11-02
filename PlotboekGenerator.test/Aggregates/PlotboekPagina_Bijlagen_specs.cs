using PlotboekGenerator.core.Events;

namespace PlotboekGenerator.test.Aggregates;

/// <summary>
/// Guards the bijlage-reference list on a pagina. The targeted <see cref="PlotboekPagina.AanpassenBijlagen"/>
/// must change only the references, and a full <see cref="PlotboekPagina.Aanpassen"/> must faithfully apply
/// (or clear) the references carried on its <see cref="PlotboekPagina.Data"/> — the reset-trap that the
/// export read model and UI depend on.
/// </summary>
public sealed class PlotboekPagina_Bijlagen_specs
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

    private static PlotboekPagina CreatePopulatedPagina()
    {
        var pagina = new PlotboekPagina();
        var plotboekId = PlotboekId.Create(Year.Create(2025), EventIds.Voorjaar);
        pagina = pagina.AanPlotboekToevoegen(plotboekId, "Voorjaar 2025", TestUser).Value;

        pagina = pagina.Aanpassen(BasisData(), TestUser).Value;
        pagina.MarkAllAsCommitted();
        return pagina;
    }

    private static PlotboekPagina.Data BasisData(params BijlageId[] bijlageIds) => new ()
    {
        Titel = "Handelaar",
        Dag = Dag.Zaterdag,
        StarttijdId = StarttijdIds.Tijd1430,
        Order = 1,
        ElevatorPitch = "Koop en verkoop",
        MeerInformatie = "<p>Details</p>",
        BijlageIds = [.. bijlageIds],
    };

    [Test]
    public void AanpassenBijlagen_sets_the_references()
    {
        var pagina = CreatePopulatedPagina();
        var refs = new[] { BijlageId.Next(), BijlageId.Next() };

        var result = pagina.AanpassenBijlagen(refs, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.BijlageRefs.Should().BeEquivalentTo(refs);
    }

    [Test]
    public void AanpassenBijlagen_preserves_all_other_content()
    {
        var pagina = CreatePopulatedPagina();
        var before = pagina.GetData();

        var after = pagina.AanpassenBijlagen([BijlageId.Next()], TestUser).Value.GetData();

        after.Titel.Should().Be(before.Titel);
        after.ElevatorPitch.Should().Be(before.ElevatorPitch);
        after.MeerInformatie.Should().Be(before.MeerInformatie);
        after.Dag.Should().Be(before.Dag);
        after.StarttijdId.Should().Be(before.StarttijdId);
        after.Order.Should().Be(before.Order);
    }

    [Test]
    public void AanpassenBijlagen_emits_a_single_bijlagen_event()
    {
        var pagina = CreatePopulatedPagina();

        var result = pagina.AanpassenBijlagen([BijlageId.Next()], TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.Buffer.Uncommitted.Should().ContainSingle()
            .Which.Should().BeOfType<PlotboekPaginaBijlagenAangepast>();
    }

    [Test]
    public void AanpassenBijlagen_is_noop_when_unchanged()
    {
        var pagina = CreatePopulatedPagina();
        var refs = new[] { BijlageId.Next() };
        pagina = pagina.AanpassenBijlagen(refs, TestUser).Value;
        pagina.MarkAllAsCommitted();

        var result = pagina.AanpassenBijlagen(refs, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.Buffer.Uncommitted.Should().BeEmpty();
    }

    [Test]
    public void AanpassenBijlagen_is_noop_on_pure_reorder()
    {
        var pagina = CreatePopulatedPagina();
        var a = BijlageId.Next();
        var b = BijlageId.Next();
        pagina = pagina.AanpassenBijlagen([a, b], TestUser).Value;
        pagina.MarkAllAsCommitted();

        // Same set, different order — references are a set, so this must not append an event.
        var result = pagina.AanpassenBijlagen([b, a], TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.Buffer.Uncommitted.Should().BeEmpty();
    }

    [Test]
    public void AanpassenBijlagen_dedupes_repeated_ids()
    {
        var pagina = CreatePopulatedPagina();
        var id = BijlageId.Next();

        var result = pagina.AanpassenBijlagen([id, id], TestUser);

        result.Value.BijlageRefs.Should().ContainSingle().Which.Should().Be(id);
    }

    [Test]
    public void Full_Aanpassen_applies_bijlage_refs_carried_on_data()
    {
        // The reset-trap guard: a full page save must honour Data.BijlageIds through the '|' chain.
        var pagina = CreatePopulatedPagina();
        pagina = pagina.AanpassenBijlagen([BijlageId.Next()], TestUser).Value;
        pagina.MarkAllAsCommitted();

        var newRefs = new[] { BijlageId.Next(), BijlageId.Next() };
        var after = pagina.Aanpassen(BasisData(newRefs), TestUser).Value;

        after.BijlageRefs.Should().BeEquivalentTo(newRefs);
    }

    [Test]
    public void Full_Aanpassen_without_refs_on_data_clears_them()
    {
        // Documents WHY BuildPaginaData must always carry BijlageIds: a full save that omits them wipes
        // the references, exactly like every other field the whole-Data Aanpassen resets.
        var pagina = CreatePopulatedPagina();
        pagina = pagina.AanpassenBijlagen([BijlageId.Next()], TestUser).Value;
        pagina.MarkAllAsCommitted();

        var after = pagina.Aanpassen(BasisData(), TestUser).Value;

        after.BijlageRefs.Should().BeEmpty();
    }
}
