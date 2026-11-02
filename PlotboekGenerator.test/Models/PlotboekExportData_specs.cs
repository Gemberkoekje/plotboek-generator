namespace PlotboekGenerator.test.Models;

public sealed class PlotboekExportData_specs
{
    [Test]
    public void DisplayName_without_titel()
    {
        var data = new Plotboek.ExportData
        {
            Jaar = Year.Create(2025),
            EventId = EventIds.Voorjaar,
        };

        data.DisplayName.Should().Be("Voorjaar 2025");
    }

    [Test]
    public void DisplayName_with_titel()
    {
        var data = new Plotboek.ExportData
        {
            Jaar = Year.Create(2025),
            EventId = EventIds.Voorjaar,
            Titel = "Avontuur in het bos",
        };

        data.DisplayName.Should().Be("Avontuur in het bos (Voorjaar 2025)");
    }

    [Test]
    public void DisplayName_with_empty_titel()
    {
        var data = new Plotboek.ExportData
        {
            Jaar = Year.Create(2025),
            EventId = "Zomer",
            Titel = string.Empty,
        };

        data.DisplayName.Should().Be("Zomer 2025");
    }

    [Test]
    public void DisplayName_with_whitespace_titel()
    {
        var data = new Plotboek.ExportData
        {
            Jaar = Year.Create(2025),
            EventId = "Zomer",
            Titel = "   ",
        };

        data.DisplayName.Should().Be("Zomer 2025");
    }

    [Test]
    public void DisplayName_evergreen()
    {
        var data = new Plotboek.ExportData
        {
            Jaar = Year.Create(1),
            EventId = EventIds.Evergreen,
        };

        data.DisplayName.Should().Be("Evergreen 1");
    }

    [TestCase(EventIds.Voorjaar)]
    [TestCase(EventIds.Najaar)]
    [TestCase(EventIds.Zomer)]
    [TestCase(EventIds.Evergreen)]
    public void DisplayName_uses_event_id(string eventId)
    {
        var data = new Plotboek.ExportData
        {
            Jaar = Year.Create(2025),
            EventId = eventId,
        };

        data.DisplayName.Should().Be($"{eventId} 2025");
    }

    [Test]
    public void Id_is_string_of_plotboek_id()
    {
        var plotboekId = PlotboekId.Create(Year.Create(2025), EventIds.Voorjaar);
        var data = new Plotboek.ExportData
        {
            PlotboekId = plotboekId,
        };

        data.Id.Should().Be(plotboekId.ToString());
    }
}
