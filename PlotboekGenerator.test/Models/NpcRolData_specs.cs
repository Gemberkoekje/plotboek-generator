using System.Text.Json;

namespace PlotboekGenerator.test.Models;

public sealed class NpcRolData_specs
{
    [Test]
    public void Empty_has_no_value()
    {
        NpcRolData.Empty.HasValue.Should().BeFalse();
    }

    [Test]
    public void Empty_ToString_returns_empty_string()
    {
        NpcRolData.Empty.ToString().Should().BeEmpty();
    }

    [Test]
    public void HasValue_is_true_when_Rol_is_set()
    {
        var data = NpcRolData.Create("guard", 1);
        data.HasValue.Should().BeTrue();
    }

    [Test]
    public void HasValue_is_false_when_Rol_is_whitespace()
    {
        var data = NpcRolData.Create("  ", 0);
        data.HasValue.Should().BeFalse();
    }

    [Test]
    public void IsRange_when_minimum_differs_from_maximum()
    {
        var data = NpcRolData.Create("guard", 1, 4);
        data.IsRange.Should().BeTrue();
    }

    [Test]
    public void IsRange_is_false_when_minimum_equals_maximum()
    {
        var data = NpcRolData.Create("guard", 2, 2);
        data.IsRange.Should().BeFalse();
    }

    [Test]
    public void IsRange_is_false_when_no_maximum()
    {
        var data = NpcRolData.Create("guard", 2);
        data.IsRange.Should().BeFalse();
    }

    [Test]
    public void IsOptional_when_minimum_is_zero_and_maximum_is_set()
    {
        var data = NpcRolData.Create("spy", 0, 2);
        data.IsOptional.Should().BeTrue();
    }

    [Test]
    public void HasCount_when_minimum_greater_than_zero()
    {
        var data = NpcRolData.Create("guard", 3);
        data.HasCount.Should().BeTrue();
    }

    [Test]
    public void HasCount_when_maximum_is_set()
    {
        var data = NpcRolData.Create("guard", 0, 5);
        data.HasCount.Should().BeTrue();
    }

    [Test]
    public void HasCount_is_false_when_both_zero_and_null()
    {
        var data = NpcRolData.Create("guard", 0);
        data.HasCount.Should().BeFalse();
    }

    [Test]
    public void ToString_exact_count()
    {
        var data = NpcRolData.Create("guards", 3);
        data.ToString().Should().Be("3 guards");
    }

    [Test]
    public void ToString_range()
    {
        var data = NpcRolData.Create("handelaars", 1, 4);
        data.ToString().Should().Be("1-4 handelaars");
    }

    [Test]
    public void ToString_no_count_shows_just_role_name()
    {
        var data = NpcRolData.Create("Vechters", 0);
        data.ToString().Should().Be("Vechters");
    }

    [Test]
    public void ToString_with_terugkerend_annotation()
    {
        var data = NpcRolData.Create("guards", 2, isTerukerend: true);
        data.ToString().Should().Be("2 guards (TERUGKEREND)");
    }

    [Test]
    public void ToString_with_eerder_gespeeld_without_name()
    {
        var data = NpcRolData.Create("edelman", 1, isEerderGespeeld: true);
        data.ToString().Should().Be("1 edelman (eerder gespeeld)");
    }

    [Test]
    public void ToString_with_eerder_gespeeld_by_name()
    {
        var data = NpcRolData.Create("edelman", 1, isEerderGespeeld: true, gespeeldDoor: "Jan");
        data.ToString().Should().Be("1 edelman (eerder gespeeld door Jan)");
    }

    [Test]
    public void ToString_with_al_gekozen_without_person()
    {
        var data = NpcRolData.Create("priester", 1, isAlGekozen: true);
        data.ToString().Should().Be("1 priester (al gekozen)");
    }

    [Test]
    public void ToString_with_al_gekozen_with_person()
    {
        var data = NpcRolData.Create("priester", 1, isAlGekozen: true, gekozenPersoon: "Piet");
        data.ToString().Should().Be("1 priester (gekozen: Piet)");
    }

    [Test]
    public void ToString_with_multiple_annotations()
    {
        var data = NpcRolData.Create("guards", 2, isTerukerend: true, isEerderGespeeld: true, gespeeldDoor: "Jan");
        data.ToString().Should().Be("2 guards (TERUGKEREND, eerder gespeeld door Jan)");
    }

    [Test]
    public void Create_clears_gespeeldDoor_when_not_eerder_gespeeld()
    {
        var data = NpcRolData.Create("guard", 1, isEerderGespeeld: false, gespeeldDoor: "Jan");
        data.GespeeldDoor.Should().BeNull();
    }

    [Test]
    public void Create_clears_gekozenPersoon_when_not_al_gekozen()
    {
        var data = NpcRolData.Create("guard", 1, isAlGekozen: false, gekozenPersoon: "Piet");
        data.GekozenPersoon.Should().BeNull();
    }

    [Test]
    public void Create_preserves_gespeeldDoor_when_eerder_gespeeld()
    {
        var data = NpcRolData.Create("guard", 1, isEerderGespeeld: true, gespeeldDoor: "Jan");
        data.GespeeldDoor.Should().Be("Jan");
    }

    [Test]
    public void Create_preserves_gekozenPersoon_when_al_gekozen()
    {
        var data = NpcRolData.Create("guard", 1, isAlGekozen: true, gekozenPersoon: "Piet");
        data.GekozenPersoon.Should().Be("Piet");
    }

    [Test]
    public void Create_with_null_rol_uses_empty_string()
    {
        var data = NpcRolData.Create(null, 0);
        data.Rol.Should().BeEmpty();
    }

    [Test]
    public void Create_sets_kledingEnSchmink()
    {
        var data = NpcRolData.Create("guard", 1, kledingEnSchmink: "malienkolder");
        data.KledingEnSchmink.Should().Be("malienkolder");
    }

    [Test]
    public void Create_sets_castingInstructie()
    {
        var data = NpcRolData.Create("guard", 1, castingInstructie: "goede roleplayer");
        data.CastingInstructie.Should().Be("goede roleplayer");
    }

    [Test]
    public void Create_clears_afstemmingToelichting_when_not_vereist()
    {
        var data = NpcRolData.Create("guard", 1, vereistAfstemming: false, afstemmingToelichting: "wordt gemarteld");
        data.AfstemmingToelichting.Should().BeNull();
    }

    [Test]
    public void Create_preserves_afstemmingToelichting_when_vereist()
    {
        var data = NpcRolData.Create("guard", 1, vereistAfstemming: true, afstemmingToelichting: "wordt gemarteld");
        data.AfstemmingToelichting.Should().Be("wordt gemarteld");
    }

    // The event schema guard fingerprints the properties of the EVENT, not the insides of the types it
    // carries, so adding fields to this record slips past it entirely. This round-trip is the only thing
    // standing between a later rename and casting data silently vanishing on replay.
    [Test]
    public void Survives_a_json_round_trip()
    {
        var data = NpcRolData.Create(
            "Guards",
            2,
            4,
            isTerukerend: true,
            isEerderGespeeld: true,
            gespeeldDoor: "Jan",
            isAlGekozen: true,
            gekozenPersoon: "Piet",
            kledingEnSchmink: "malienkolder, wapens",
            castingInstructie: "goede roleplayer, stevig postuur",
            vereistAfstemming: true,
            afstemmingToelichting: "wordt gemarteld in scène 2");

        var restored = JsonSerializer.Deserialize<NpcRolData>(JsonSerializer.Serialize(data))!;

        restored.Should().Be(data, "every field must survive the trip through the event store");
    }

    // Old event JSON simply has no casting properties; they must come back as null/false rather than blow up.
    [Test]
    public void Deserializes_pre_casting_json_with_empty_casting_fields()
    {
        const string json = """{"Rol":"Guards","MinimumAantal":2,"MaximumAantal":4,"KledingEnSchmink":"malienkolder"}""";

        var restored = JsonSerializer.Deserialize<NpcRolData>(json)!;

        restored.CastingInstructie.Should().BeNull();
        restored.VereistAfstemming.Should().BeFalse();
        restored.AfstemmingToelichting.Should().BeNull();
        restored.KledingEnSchmink.Should().Be("malienkolder", "the existing fields must still be read");
    }
}

