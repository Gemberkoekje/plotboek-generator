namespace PlotboekGenerator.test.Models;

/// <summary>
/// The NPC texts that end up on the page and in the Word export speak the reader's language. NUnit puts
/// the thread's UI culture back after each spec.
/// </summary>
[SetUICulture("en")]
public sealed class AantalNpcsData_English_specs
{
    [Test]
    public void The_total_of_several_roles_reads_in_english()
    {
        var data = AantalNpcsData.Create([NpcRolData.Create("guard", 2), NpcRolData.Create("noble", 1, isTerukerend: true)]);

        data.ToString().Should().Be("• 2 guard\n• 1 noble (RECURRING)\nTotal: 3 NPCs");
    }

    [Test]
    public void One_role_among_all_npcs_is_singular_in_english()
    {
        var data = AantalNpcsData.Create([NpcRolData.Create("Fighters", 0)], alleNpcs: true);

        data.ToShortString().Should().Be("ALL NPCs (1 role)");
    }

    [Test]
    public void No_npcs_needed_reads_in_english()
        => AantalNpcsData.Create([], geenNpcsNodig: true).ToKledingEnSchminkString().Should().Be("No NPCs needed");

    [Test]
    public void The_export_cell_headings_read_in_english()
    {
        var data = AantalNpcsData.Create(
        [
            NpcRolData.Create(
                "guard",
                2,
                kledingEnSchmink: "chainmail",
                castingInstructie: "sturdy build",
                vereistAfstemming: true,
                afstemmingToelichting: "is tortured in scene 2"),
        ]);

        data.ToKledingEnSchminkString().Should().Be(
            "\U0001F91D Needs agreement beforehand:\nguard: is tortured in scene 2\n\n"
            + "Casting:\nguard: sturdy build\n\n"
            + "Costume & make-up:\nguard: chainmail");
    }
}
