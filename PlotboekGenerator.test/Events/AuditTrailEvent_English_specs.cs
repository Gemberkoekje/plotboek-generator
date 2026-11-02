using PlotboekGenerator.core.Events;

namespace PlotboekGenerator.test.Events;

/// <summary>
/// The change history speaks the reader's language: the words come from the resources, the markup and
/// the values stay as they are. NUnit puts the thread's UI culture back after each spec.
/// </summary>
[SetUICulture("en")]
public sealed class AuditTrailEvent_English_specs
{
    [Test]
    public void A_location_change_reads_in_english()
    {
        var e = new PlotboekPaginaLocatieEncounterAangepastV2
        {
            Locatie = LocatieEncounter.Questveld,
            Specificatie = "By the tree",
            Who = "user-1",
        };

        e.What.Should().Be("<strong>Encounter location changed:</strong> Quest field (By the tree)");
    }

    [Test]
    public void An_emptied_npc_list_reads_in_english()
    {
        var e = new PlotboekPaginaAantalNpcsAangepastV2
        {
            Rollen = [],
            Who = "user-1",
        };

        e.What.Should().Be("<strong>Number of NPCs changed:</strong> (empty)");
    }

    [Test]
    public void The_npc_details_read_in_english()
    {
        var e = new PlotboekPaginaAantalNpcsAangepastV2
        {
            Rollen =
            [
                NpcRolData.Create(
                    "guard",
                    2,
                    isEerderGespeeld: true,
                    gespeeldDoor: "Jan",
                    vereistAfstemming: true,
                    afstemmingToelichting: "is tortured in scene 2"),
            ],
            Who = "user-1",
        };

        e.Details.Should().Contain("played before by Jan", "the role's annotations are translated")
            .And.Contain("Needs agreement: is tortured in scene 2", "the agreement line is translated");
    }

    [Test]
    public void A_change_of_days_names_the_days_in_english()
    {
        var e = new PlotboekDagenAangepast
        {
            StartDag = Dag.Vrijdag,
            EindDag = Dag.Zondag,
            Who = "user-1",
        };

        e.What.Should().Be("<strong>Days changed:</strong> Friday to Sunday");
    }

    [Test]
    [SetUICulture("nl")]
    public void The_dutch_text_stays_as_it_was()
    {
        var e = new PlotboekDagenAangepast
        {
            StartDag = Dag.Vrijdag,
            EindDag = Dag.Zondag,
            Who = "user-1",
        };

        e.What.Should().Be("<strong>Dagen aangepast:</strong> Vrijdag t/m Zondag", "Dutch is the neutral language");
    }
}
