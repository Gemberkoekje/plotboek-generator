using PlotboekGenerator.core.Events;

namespace PlotboekGenerator.test.Events;

public sealed class PlotboekEvent_What_specs
{
    [Test]
    public void Aangemaakt_what_contains_details()
    {
        var e = new PlotboekAangemaaktV2
        {
            Jaar = Year.Create(2025),
            EventId = EventIds.Voorjaar,
            StartDag = Dag.Vrijdag,
            EindDag = Dag.Zondag,
            Who = "user-1",
        };

        e.What.Should().Contain("Aangemaakt");
        e.What.Should().Contain("2025");
        e.What.Should().Contain("Voorjaar");
        e.What.Should().Contain("Vrijdag");
        e.What.Should().Contain("Zondag");
    }

    [Test]
    public void TitelAangepast_what_contains_titel()
    {
        var e = new PlotboekTitelAangepast
        {
            Titel = "Avontuur in het bos",
            Who = "user-1",
        };

        e.What.Should().Contain("Titel aangepast");
        e.What.Should().Contain("Avontuur in het bos");
    }

    [Test]
    public void DagenAangepast_what_contains_dagen()
    {
        var e = new PlotboekDagenAangepast
        {
            StartDag = Dag.Donderdag,
            EindDag = Dag.Zaterdag,
            Who = "user-1",
        };

        e.What.Should().Contain("Dagen aangepast");
        e.What.Should().Contain("Donderdag");
        e.What.Should().Contain("Zaterdag");
    }

    [Test]
    public void DatumsAangepast_what_contains_datums()
    {
        var e = new PlotboekDatumsAangepast
        {
            StartDatum = new DateOnly(2025, 7, 4),
            EindDatum = new DateOnly(2025, 7, 6),
            Who = "user-1",
        };

        e.What.Should().Contain("Datums aangepast");
    }

    [Test]
    public void DatumsAangepast_what_with_null_datums()
    {
        var e = new PlotboekDatumsAangepast
        {
            StartDatum = null,
            EindDatum = null,
            Who = "user-1",
        };

        e.What.Should().Contain("Datums aangepast");
    }

    [Test]
    public void SamenvattingAlgemeenPlotAangepast_what()
    {
        var e = new PlotboekSamenvattingAlgemeenPlotAangepast
        {
            SamenvattingAlgemeenPlot = "<p>Het grote verhaal</p>",
            Who = "user-1",
        };

        e.What.Should().Contain("Samenvatting Algemeen Plot aangepast");
    }

    [Test]
    public void SamenvattingAlgemeenPlotAangepast_details_with_content()
    {
        var e = new PlotboekSamenvattingAlgemeenPlotAangepast
        {
            SamenvattingAlgemeenPlot = "<p>Het grote verhaal</p>",
            Who = "user-1",
        };

        e.Details.Should().Contain("Het grote verhaal");
    }

    [Test]
    public void SamenvattingAlgemeenPlotAangepast_details_empty_returns_null()
    {
        var e = new PlotboekSamenvattingAlgemeenPlotAangepast
        {
            SamenvattingAlgemeenPlot = "   ",
            Who = "user-1",
        };

        e.Details.Should().BeNull();
    }

    [Test]
    public void GoogleDriveFolderUrlAangepast_what()
    {
        var e = new PlotboekGoogleDriveFolderUrlAangepast
        {
            GoogleDriveFolderUrl = "https://drive.google.com/folder/abc",
            Who = "user-1",
        };

        e.What.Should().Contain("Google Drive folder URL aangepast");
    }

    [Test]
    public void GoogleDriveFileUrlAangepast_what()
    {
        var e = new PlotboekGoogleDriveFileUrlAangepast
        {
            GoogleDriveFileUrl = "https://drive.google.com/file/xyz",
            Who = "user-1",
        };

        e.What.Should().Contain("Google Drive bestand URL aangepast");
    }

    [Test]
    public void VolledigKlaarAangepast_what()
    {
        var e = new PlotboekVolledigKlaarAangepast
        {
            IsVolledigKlaar = true,
            Who = "user-1",
        };

        e.What.Should().Contain("Plotboek gemarkeerd als volledig klaar");
    }

    [Test]
    public void BijlagenAangepast_what_contains_count()
    {
        var e = new PlotboekBijlagenAangepast
        {
            Bijlagen =
            [
                BijlageData.Create(BijlageId.Next(), "Prijslijst", "<p>Brazen Bull</p>", 0),
                BijlageData.Create(BijlageId.Next(), "Statblock", "<p>AC 15</p>", 1),
            ],
            Who = "user-1",
        };

        e.What.Should().Contain("Bijlagen aangepast");
        e.What.Should().Contain("2");
    }
}
