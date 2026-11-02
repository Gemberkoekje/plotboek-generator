namespace PlotboekGenerator.test.Models;

public sealed class AantalNpcsData_specs
{
    [Test]
    public void Empty_has_no_value()
    {
        AantalNpcsData.Empty.HasValue.Should().BeFalse();
    }

    [Test]
    public void HasValue_when_alle_npcs()
    {
        var data = AantalNpcsData.Create([], alleNpcs: true);
        data.HasValue.Should().BeTrue();
    }

    [Test]
    public void HasValue_when_roles_exist()
    {
        var data = AantalNpcsData.Create([NpcRolData.Create("guard", 2)]);
        data.HasValue.Should().BeTrue();
    }

    [Test]
    public void HasValue_is_false_when_only_empty_roles()
    {
        var data = AantalNpcsData.Create([NpcRolData.Empty]);
        data.HasValue.Should().BeFalse();
    }

    [Test]
    public void Create_filters_empty_roles()
    {
        var data = AantalNpcsData.Create([NpcRolData.Create("guard", 2), NpcRolData.Empty]);
        data.Rollen.Should().ContainSingle();
    }

    [Test]
    public void Create_with_null_returns_empty_roles()
    {
        var data = AantalNpcsData.Create(null);
        data.Rollen.Should().BeEmpty();
    }

    [Test]
    public void TotaalMinimum_sums_minimums()
    {
        var data = AantalNpcsData.Create([
            NpcRolData.Create("guard", 2),
            NpcRolData.Create("spy", 1),
        ]);
        data.TotaalMinimum.Should().Be(3);
    }

    [Test]
    public void TotaalMaximum_sums_maximums()
    {
        var data = AantalNpcsData.Create([
            NpcRolData.Create("guard", 2, 4),
            NpcRolData.Create("spy", 1, 3),
        ]);
        data.TotaalMaximum.Should().Be(7);
    }

    [Test]
    public void TotaalMinimum_is_zero_when_no_roles()
    {
        AantalNpcsData.Empty.TotaalMinimum.Should().Be(0);
    }

    [Test]
    public void ToString_empty_returns_empty()
    {
        AantalNpcsData.Empty.ToString().Should().BeEmpty();
    }

    [Test]
    public void ToString_alle_npcs()
    {
        var data = AantalNpcsData.Create([], alleNpcs: true);
        data.ToString().Should().Contain("ALLE NPCs");
    }

    [Test]
    public void ToString_single_role()
    {
        var data = AantalNpcsData.Create([NpcRolData.Create("guards", 2)]);
        data.ToString().Should().Contain("2 guards");
    }

    [Test]
    public void ToString_multiple_roles_shows_total()
    {
        var data = AantalNpcsData.Create([
            NpcRolData.Create("guards", 2),
            NpcRolData.Create("spies", 1),
        ]);
        var result = data.ToString();
        result.Should().Contain("Totaal: 3 NPC's");
    }

    [Test]
    public void ToString_alle_npcs_with_roles()
    {
        var data = AantalNpcsData.Create([NpcRolData.Create("Vechters", 0)], alleNpcs: true);
        var result = data.ToString();
        result.Should().Contain("ALLE NPCs");
        result.Should().Contain("Vechters");
    }

    [Test]
    public void ToShortString_empty_returns_empty()
    {
        AantalNpcsData.Empty.ToShortString().Should().BeEmpty();
    }

    [Test]
    public void ToShortString_alle_npcs_without_roles()
    {
        var data = AantalNpcsData.Create([], alleNpcs: true);
        data.ToShortString().Should().Be("ALLE NPCs");
    }

    [Test]
    public void ToShortString_alle_npcs_with_roles()
    {
        var data = AantalNpcsData.Create([NpcRolData.Create("guard", 1)], alleNpcs: true);
        data.ToShortString().Should().Be("ALLE NPCs (1 rollen)");
    }

    [Test]
    public void ToShortString_single_role_exact()
    {
        var data = AantalNpcsData.Create([NpcRolData.Create("guards", 3)]);
        data.ToShortString().Should().Be("3 guards");
    }

    [Test]
    public void ToShortString_single_role_range()
    {
        var data = AantalNpcsData.Create([NpcRolData.Create("guards", 2, 4)]);
        data.ToShortString().Should().Be("2-4 guards");
    }

    [Test]
    public void ToShortString_multiple_roles_summary()
    {
        var data = AantalNpcsData.Create([
            NpcRolData.Create("guards", 2, 4),
            NpcRolData.Create("spies", 1, 3),
        ]);
        data.ToShortString().Should().Be("3-7 NPC's (2 rollen)");
    }

    [Test]
    public void ToKledingEnSchminkString_empty_when_no_kleding()
    {
        var data = AantalNpcsData.Create([NpcRolData.Create("guard", 1)]);
        data.ToKledingEnSchminkString().Should().BeEmpty();
    }

    [Test]
    public void ToKledingEnSchminkString_formats_roles_with_kleding()
    {
        var data = AantalNpcsData.Create([
            NpcRolData.Create("Guards", 2, kledingEnSchmink: "malienkolder"),
            NpcRolData.Create("Edelman", 1, kledingEnSchmink: "deftige kleding"),
        ]);
        data.ToKledingEnSchminkString().Should().Be("Guards: malienkolder\nEdelman: deftige kleding");
    }

    [Test]
    public void ToKledingEnSchminkString_skips_roles_without_kleding()
    {
        var data = AantalNpcsData.Create([
            NpcRolData.Create("Guards", 2, kledingEnSchmink: "malienkolder"),
            NpcRolData.Create("Spy", 1),
        ]);
        data.ToKledingEnSchminkString().Should().Be("Guards: malienkolder");
    }

    // Only clothing filled in — by far the most common case — so the cell must look exactly like it did
    // before casting existed: no headings, no blank lines.
    [Test]
    public void ToKledingEnSchminkString_single_block_has_no_headings()
    {
        var data = AantalNpcsData.Create([
            NpcRolData.Create("Guards", 2, kledingEnSchmink: "malienkolder"),
            NpcRolData.Create("Edelman", 1, kledingEnSchmink: "deftige kleding"),
        ]);
        data.ToKledingEnSchminkString().Should().Be("Guards: malienkolder\nEdelman: deftige kleding");
    }

    // Casting alone still gets its heading: the cell is labelled "Kleding & Schmink" in the template, so
    // without one this would read as a costume note.
    [Test]
    public void ToKledingEnSchminkString_casting_only_keeps_its_heading()
    {
        var data = AantalNpcsData.Create([NpcRolData.Create("Edelman", 1, castingInstructie: "vrouwelijk uiterlijk")]);
        data.ToKledingEnSchminkString().Should().Be("Casting:\nEdelman: vrouwelijk uiterlijk");
    }

    // The case that matters most: a role that needs nothing but a consent conversation. Dropping the
    // heading here would file it under clothing.
    [Test]
    public void ToKledingEnSchminkString_afstemming_only_keeps_its_heading()
    {
        var data = AantalNpcsData.Create([
            NpcRolData.Create("Beul", 1, vereistAfstemming: true, afstemmingToelichting: "wordt gemarteld in scène 2"),
        ]);

        data.ToKledingEnSchminkString().Should().Be(
            "\U0001F91D Vereist afstemming vooraf:\nBeul: wordt gemarteld in scène 2",
            "an agreement note may never pass for a costume note");
    }

    [Test]
    public void ToKledingEnSchminkString_afstemming_only_without_toelichting_keeps_its_heading()
    {
        var data = AantalNpcsData.Create([NpcRolData.Create("Beul", 1, vereistAfstemming: true)]);

        data.ToKledingEnSchminkString().Should().Be("\U0001F91D Vereist afstemming vooraf:\nBeul");
    }

    // Line breaks are the block/line separators in this cell, so they must never survive inside a value —
    // otherwise a pasted or hand-POSTed newline forges a block boundary or a heading.
    [Test]
    public void ToKledingEnSchminkString_collapses_newlines_inside_a_value()
    {
        var data = AantalNpcsData.Create([
            NpcRolData.Create("Guards", 2, castingInstructie: "aardig\n\n\U0001F91D Vereist afstemming vooraf:\nBeul: nep"),
        ]);

        data.ToKledingEnSchminkString().Should().Be(
            "Casting:\nGuards: aardig  \U0001F91D Vereist afstemming vooraf: Beul: nep",
            "a forged heading must stay on one line instead of becoming a real block");
    }

    [TestCase("regel1\nregel2")]
    [TestCase("regel1\r\nregel2")]
    [TestCase("regel1\rregel2")]
    public void ToKledingEnSchminkString_never_emits_a_blank_line_from_user_text(string kleding)
    {
        var data = AantalNpcsData.Create([NpcRolData.Create("Guards", 2, kledingEnSchmink: kleding)]);

        data.ToKledingEnSchminkString().Should().Be("Guards: regel1 regel2");
    }

    [Test]
    public void ToKledingEnSchminkString_collapses_newlines_in_the_role_name()
    {
        var data = AantalNpcsData.Create([NpcRolData.Create("Guards\nEdelman", 2, kledingEnSchmink: "malienkolder")]);

        data.ToKledingEnSchminkString().Should().Be("Guards Edelman: malienkolder");
    }

    [Test]
    public void ToKledingEnSchminkString_includes_role_with_only_casting()
    {
        var data = AantalNpcsData.Create([
            NpcRolData.Create("Guards", 2, kledingEnSchmink: "malienkolder"),
            NpcRolData.Create("Edelman", 1, castingInstructie: "vrouwelijk uiterlijk"),
        ]);

        var result = data.ToKledingEnSchminkString();

        result.Should().Be(
            "Casting:\nEdelman: vrouwelijk uiterlijk\n\nKleding & schmink:\nGuards: malienkolder",
            "a role with only casting info may not fall out of the cell");
    }

    [Test]
    public void ToKledingEnSchminkString_three_blocks_separated_by_a_blank_line()
    {
        var data = AantalNpcsData.Create([
            NpcRolData.Create(
                "Guards",
                2,
                kledingEnSchmink: "malienkolder, wapens, schilden",
                castingInstructie: "goede roleplayer, stevig postuur",
                vereistAfstemming: true,
                afstemmingToelichting: "wordt gemarteld in scène 2"),
            NpcRolData.Create("Edelman", 1, kledingEnSchmink: "deftige kleding", castingInstructie: "vrouwelijk uiterlijk"),
        ]);

        data.ToKledingEnSchminkString().Should().Be(
            "\U0001F91D Vereist afstemming vooraf:\nGuards: wordt gemarteld in scène 2\n\n"
            + "Casting:\nGuards: goede roleplayer, stevig postuur\nEdelman: vrouwelijk uiterlijk\n\n"
            + "Kleding & schmink:\nGuards: malienkolder, wapens, schilden\nEdelman: deftige kleding");
    }

    [Test]
    public void ToKledingEnSchminkString_afstemming_without_toelichting_shows_the_role_name()
    {
        var data = AantalNpcsData.Create([
            NpcRolData.Create("Guards", 2, kledingEnSchmink: "malienkolder", vereistAfstemming: true),
        ]);

        data.ToKledingEnSchminkString().Should().Be(
            "\U0001F91D Vereist afstemming vooraf:\nGuards\n\nKleding & schmink:\nGuards: malienkolder");
    }

    [Test]
    public void ToKledingEnSchminkString_empty_when_no_role_has_any_of_the_three()
    {
        var data = AantalNpcsData.Create([NpcRolData.Create("Guards", 2)]);
        data.ToKledingEnSchminkString().Should().BeEmpty();
    }

    // Pins down the GeenNpcsNodig short-circuit, which until now had no coverage on this method at all.
    [Test]
    public void ToKledingEnSchminkString_reports_geen_npcs_nodig()
    {
        var data = AantalNpcsData.Create([], geenNpcsNodig: true);
        data.ToKledingEnSchminkString().Should().Be("Geen NPC's nodig");
    }

    [Test]
    public void HeeftAfstemmingNodig_is_false_without_roles_that_need_it()
    {
        var data = AantalNpcsData.Create([NpcRolData.Create("guard", 2, castingInstructie: "stevig postuur")]);
        data.HeeftAfstemmingNodig.Should().BeFalse();
    }

    [Test]
    public void HeeftAfstemmingNodig_is_true_when_one_role_needs_it()
    {
        var data = AantalNpcsData.Create([
            NpcRolData.Create("guard", 2),
            NpcRolData.Create("slachtoffer", 1, vereistAfstemming: true, afstemmingToelichting: "wordt gemarteld"),
        ]);
        data.HeeftAfstemmingNodig.Should().BeTrue();
    }

    [Test]
    public void HeeftAfstemmingNodig_is_false_when_empty()
    {
        AantalNpcsData.Empty.HeeftAfstemmingNodig.Should().BeFalse();
    }

    [Test]
    public void Not_equals_different_casting()
    {
        var a = AantalNpcsData.Create([NpcRolData.Create("guard", 2, castingInstructie: "goede roleplayer")]);
        var b = AantalNpcsData.Create([NpcRolData.Create("guard", 2)]);
        a.Should().NotBe(b, "changing only the casting must still produce an event");
    }

    [Test]
    public void GeenNpcsNodig_has_value()
    {
        var data = AantalNpcsData.Create([], geenNpcsNodig: true);
        data.GeenNpcsNodig.Should().BeTrue();
        data.HasValue.Should().BeTrue();
    }

    [Test]
    public void GeenNpcsNodig_clears_alle_npcs_and_roles()
    {
        var data = AantalNpcsData.Create([NpcRolData.Create("guard", 2)], alleNpcs: true, geenNpcsNodig: true);
        data.AlleNpcs.Should().BeFalse();
        data.Rollen.Should().BeEmpty();
    }

    [Test]
    public void GeenNpcsNodig_display_strings()
    {
        var data = AantalNpcsData.Create([], geenNpcsNodig: true);
        data.ToString().Should().Be("Geen NPC's nodig");
        data.ToShortString().Should().Be("0");
        data.ToCountDisplayString().Should().Be("0");
    }

    [Test]
    public void Not_equals_different_geenNpcsNodig()
    {
        var a = AantalNpcsData.Create([], geenNpcsNodig: true);
        var b = AantalNpcsData.Empty;
        a.Should().NotBe(b);
    }

    [Test]
    public void Equals_same_data()
    {
        var a = AantalNpcsData.Create([NpcRolData.Create("guard", 2)], alleNpcs: false);
        var b = AantalNpcsData.Create([NpcRolData.Create("guard", 2)], alleNpcs: false);
        a.Should().Be(b);
    }

    [Test]
    public void Not_equals_different_alleNpcs()
    {
        var a = AantalNpcsData.Create([], alleNpcs: true);
        var b = AantalNpcsData.Create([], alleNpcs: false);
        a.Should().NotBe(b);
    }

    [Test]
    public void Not_equals_different_roles()
    {
        var a = AantalNpcsData.Create([NpcRolData.Create("guard", 2)]);
        var b = AantalNpcsData.Create([NpcRolData.Create("spy", 1)]);
        a.Should().NotBe(b);
    }
}

