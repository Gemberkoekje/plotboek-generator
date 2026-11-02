namespace PlotboekGenerator.test.Pages;

public sealed class PlotboekModel_CaptureEditablePlotboekTextFields_specs
{
    private static PlotboekModel CreateModel()
        => new (null!, null!, null!, null!, null!, null!, null!, null!, null!);

    [Test]
    public void Returns_all_expected_keys()
    {
        var _sut = CreateModel();

        var result = _sut.CaptureEditablePlotboekTextFields();

        result.Should().ContainKey("Titel");
        result.Should().ContainKey("Samenvatting Algemeen Plot");
        result.Should().ContainKey("Google Drive Folder URL");
        result.Should().ContainKey("NPCs Donderdag");
        result.Should().ContainKey("NPCs Vrijdag");
        result.Should().ContainKey("NPCs Zaterdag");
        result.Should().ContainKey("NPCs Zondag");
    }

    [Test]
    public void Captures_values_from_editable_fields()
    {
        var _sut = CreateModel();
        _sut.EditablePlotboekTitel = "Plotboek 2026";
        _sut.EditableSamenvattingAlgemeenPlot = "<p>Het plot</p>";
        _sut.EditableGoogleDriveFolderUrl = "https://drive.google.com/drive/folders/abc";
        _sut.EditableNpcsZaterdag = 12;

        var result = _sut.CaptureEditablePlotboekTextFields();

        result["Titel"].Should().Be("Plotboek 2026");
        result["Samenvatting Algemeen Plot"].Should().Be("<p>Het plot</p>");
        result["Google Drive Folder URL"].Should().Be("https://drive.google.com/drive/folders/abc");
        result["NPCs Zaterdag"].Should().Be("12");
    }

    [Test]
    public void Captures_bijlage_name_and_content_so_a_rejected_save_can_offer_them_back()
    {
        var _sut = CreateModel();
        _sut.EditableBijlagen =
        [
            new PlotboekModel.BijlageInput { Naam = "Prijslijst", InhoudHtml = "<p>bier 2 kopermunten</p>" },
        ];

        var result = _sut.CaptureEditablePlotboekTextFields();

        result["Bijlage 1 naam"].Should().Be("Prijslijst");
        result["Bijlage 1 inhoud"].Should().Be("<p>bier 2 kopermunten</p>");
    }

    [Test]
    public void Captures_every_bijlage_by_its_position()
    {
        var _sut = CreateModel();
        _sut.EditableBijlagen =
        [
            new PlotboekModel.BijlageInput { Naam = "Eerste", InhoudHtml = "<p>a</p>" },
            new PlotboekModel.BijlageInput { Naam = "Tweede", InhoudHtml = "<p>b</p>" },
        ];

        var result = _sut.CaptureEditablePlotboekTextFields();

        result["Bijlage 1 inhoud"].Should().Be("<p>a</p>");
        result["Bijlage 2 inhoud"].Should().Be("<p>b</p>");
    }

    [Test]
    public void Captures_the_content_of_a_bijlage_that_has_no_name_yet()
    {
        var _sut = CreateModel();
        _sut.EditableBijlagen =
        [
            new PlotboekModel.BijlageInput { Naam = string.Empty, InhoudHtml = "<p>getypte tekst</p>" },
        ];

        var result = _sut.CaptureEditablePlotboekTextFields();

        result["Bijlage 1 inhoud"].Should().Be("<p>getypte tekst</p>", "this is exactly the text that used to disappear");
    }

    [Test]
    public void Captures_empty_editor_markup_as_null_so_it_is_not_offered_back_as_a_change()
    {
        var _sut = CreateModel();
        _sut.EditableBijlagen =
        [
            new PlotboekModel.BijlageInput { Naam = "Prijslijst", InhoudHtml = "<p><br></p>" },
        ];

        var result = _sut.CaptureEditablePlotboekTextFields();

        result["Bijlage 1 inhoud"].Should().BeNull();
    }

    [Test]
    public void Captures_no_bijlage_keys_when_there_are_no_bijlagen()
    {
        var _sut = CreateModel();

        var result = _sut.CaptureEditablePlotboekTextFields();

        result.Keys.Should().NotContain(k => k.StartsWith("Bijlage ", StringComparison.Ordinal));
    }

    [TestCase("Samenvatting Algemeen Plot", true)]
    [TestCase("Bijlage 1 inhoud", true)]
    [TestCase("Bijlage 12 inhoud", true)]
    [TestCase("Bijlage 1 naam", false)]
    [TestCase("Titel", false)]
    [TestCase("Google Drive Folder URL", false)]
    [TestCase("NPCs Zaterdag", false)]
    public void Knows_which_captured_fields_hold_html(string label, bool expected)
    {
        PlotboekModel.IsHtmlSettingsChangeLabel(label).Should().Be(expected);
    }

    [Test]
    public void Labels_every_captured_bijlage_content_field_as_html()
    {
        var _sut = CreateModel();
        _sut.EditableBijlagen =
        [
            new PlotboekModel.BijlageInput { Naam = "Prijslijst", InhoudHtml = "<p>a</p>" },
            new PlotboekModel.BijlageInput { Naam = "Statblock", InhoudHtml = "<p>b</p>" },
        ];

        var result = _sut.CaptureEditablePlotboekTextFields();

        PlotboekModel.IsHtmlSettingsChangeLabel(PlotboekModel.BijlageInhoudLabel(1)).Should().BeTrue();
        PlotboekModel.IsHtmlSettingsChangeLabel(PlotboekModel.BijlageInhoudLabel(2)).Should().BeTrue();
        result.Should().ContainKey(PlotboekModel.BijlageInhoudLabel(1));
        result.Should().ContainKey(PlotboekModel.BijlageInhoudLabel(2));
    }
}
