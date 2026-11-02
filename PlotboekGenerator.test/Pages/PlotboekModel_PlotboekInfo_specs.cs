namespace PlotboekGenerator.test.Pages;

public sealed class PlotboekModel_PlotboekInfo_specs
{
    [TestCase("Voorjaar", "VJ")]
    [TestCase("Zomer", "ZO")]
    [TestCase("Najaar", "NJ")]
    public void DisplayName_uses_the_configured_abbreviation(string eventId, string abbreviation)
    {
        var info = new PlotboekModel.PlotboekInfo
        {
            Id = PlotboekId.Create(Year.Create(2025), eventId),
            Jaar = Year.Create(2025),
            EventId = eventId,
            EventAbbreviation = abbreviation,
        };

        info.DisplayName.Should().Be($"2025 - {abbreviation}");
    }

    [Test]
    public void DisplayName_falls_back_to_the_event_id_without_an_abbreviation()
    {
        var info = new PlotboekModel.PlotboekInfo
        {
            Id = PlotboekId.Create(Year.Create(2025), "Voorjaar"),
            Jaar = Year.Create(2025),
            EventId = "Voorjaar",
        };

        info.DisplayName.Should().Be("2025 - Voorjaar", "the abbreviation comes from the event's config, which was not loaded");
    }

    [Test]
    public void DisplayName_Evergreen()
    {
        var info = new PlotboekModel.PlotboekInfo
        {
            Id = PlotboekId.CreateEvergreen(),
            Jaar = Year.Create(1),
            EventId = "Evergreen",
        };

        info.DisplayName.Should().Be("Evergreen");
    }
}
