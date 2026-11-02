namespace PlotboekGenerator.test.Models;

public sealed class LocatieEncounterData_specs
{
    [Test]
    public void Empty_has_no_value()
    {
        LocatieEncounterData.Empty.HasValue.Should().BeFalse();
    }

    [Test]
    public void HasValue_when_locatie_is_set()
    {
        var data = LocatieEncounterData.Create(LocatieEncounter.Hoofdveld);
        data.HasValue.Should().BeTrue();
    }

    [Test]
    public void RequiresSpecificatie_for_Groeperingsveld()
    {
        var data = LocatieEncounterData.Create(LocatieEncounter.Groeperingsveld);
        data.RequiresSpecificatie.Should().BeTrue();
    }

    [Test]
    public void RequiresSpecificatie_for_Questveld()
    {
        var data = LocatieEncounterData.Create(LocatieEncounter.Questveld);
        data.RequiresSpecificatie.Should().BeTrue();
    }

    [Test]
    public void Does_not_require_specificatie_for_Questveld_with_a_picked_locatie()
    {
        var data = LocatieEncounterData.Create(LocatieEncounter.Questveld, locatieId: "Leeuwenkuil");

        data.RequiresSpecificatie.Should().BeFalse("the picked location replaces the free text");
        data.HasQuestLocatieKeuze.Should().BeTrue();
    }

    [Test]
    public void Create_keeps_the_locatie_id_only_for_Questveld()
    {
        LocatieEncounterData.Create(LocatieEncounter.Questveld, locatieId: "Leeuwenkuil").LocatieId.Should().Be("Leeuwenkuil");
        LocatieEncounterData.Create(LocatieEncounter.Groeperingsveld, locatieId: "Leeuwenkuil").LocatieId.Should().BeNull();
        LocatieEncounterData.Create(LocatieEncounter.Hoofdveld, locatieId: "Leeuwenkuil").LocatieId.Should().BeNull();
        LocatieEncounterData.Create(LocatieEncounter.None, locatieId: "Leeuwenkuil").LocatieId.Should().BeNull();
    }

    [Test]
    public void Create_drops_the_specificatie_when_a_questlocatie_is_picked()
    {
        var data = LocatieEncounterData.Create(LocatieEncounter.Questveld, "Oude vrije tekst", "Leeuwenkuil");

        data.Specificatie.Should().BeNull("only the Anders-branch keeps free text");
        data.LocatieId.Should().Be("Leeuwenkuil");
    }

    [Test]
    public void Blank_locatie_id_counts_as_the_Anders_branch()
    {
        var data = LocatieEncounterData.Create(LocatieEncounter.Questveld, "Pad A", "   ");

        data.LocatieId.Should().BeNull();
        data.Specificatie.Should().Be("Pad A");
        data.HasQuestLocatieKeuze.Should().BeFalse();
    }

    [Test]
    public void ToString_Questveld_with_a_picked_locatie_shows_no_free_text()
    {
        var data = LocatieEncounterData.Create(LocatieEncounter.Questveld, locatieId: "Leeuwenkuil");

        data.ToString().Should().Be("Questveld", "core cannot resolve the display name; the UI and export do that");
    }

    [Test]
    public void Does_not_require_specificatie_for_Hoofdveld()
    {
        var data = LocatieEncounterData.Create(LocatieEncounter.Hoofdveld);
        data.RequiresSpecificatie.Should().BeFalse();
    }

    [Test]
    public void Create_preserves_specificatie_for_Groeperingsveld()
    {
        var data = LocatieEncounterData.Create(LocatieEncounter.Groeperingsveld, "Noord");
        data.Specificatie.Should().Be("Noord");
    }

    [Test]
    public void Create_preserves_specificatie_for_Questveld()
    {
        var data = LocatieEncounterData.Create(LocatieEncounter.Questveld, "Pad A");
        data.Specificatie.Should().Be("Pad A");
    }

    [Test]
    public void Create_clears_specificatie_for_Hoofdveld()
    {
        var data = LocatieEncounterData.Create(LocatieEncounter.Hoofdveld, "Ignored");
        data.Specificatie.Should().BeNull();
    }

    [Test]
    public void Create_clears_specificatie_for_None()
    {
        var data = LocatieEncounterData.Create(LocatieEncounter.None, "Ignored");
        data.Specificatie.Should().BeNull();
    }

    [Test]
    public void ToString_empty_returns_empty_string()
    {
        LocatieEncounterData.Empty.ToString().Should().BeEmpty();
    }

    [Test]
    public void ToString_Hoofdveld_without_specificatie()
    {
        var data = LocatieEncounterData.Create(LocatieEncounter.Hoofdveld);
        data.ToString().Should().Be("Hoofdveld");
    }

    [Test]
    public void ToString_Groeperingsveld_with_specificatie()
    {
        var data = LocatieEncounterData.Create(LocatieEncounter.Groeperingsveld, "Noord");
        data.ToString().Should().Be("Groeperingsveld (Noord)");
    }

    [Test]
    public void ToString_Questveld_with_specificatie()
    {
        var data = LocatieEncounterData.Create(LocatieEncounter.Questveld, "Pad A");
        data.ToString().Should().Be("Questveld (Pad A)");
    }

    [Test]
    public void ToString_Groeperingsveld_without_specificatie()
    {
        var data = LocatieEncounterData.Create(LocatieEncounter.Groeperingsveld);
        data.ToString().Should().Be("Groeperingsveld");
    }
}

