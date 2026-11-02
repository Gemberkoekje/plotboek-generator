using PlotboekGenerator.core;

namespace PlotboekGenerator.test.Pages;

public sealed class PlotboekModel_CaptureEditableTextFields_specs
{
    private static PlotboekModel CreateModel()
        => new (null!, null!, null!, null!, null!, null!, null!, null!, null!);

    [Test]
    public void Returns_all_expected_keys()
    {
        var model = CreateModel();

        var result = model.CaptureEditableTextFields();

        result.Should().ContainKey("Titel");
        result.Should().ContainKey("Elevator Pitch");
        result.Should().ContainKey("Samenvatting Plot");
        result.Should().ContainKey("Meer Informatie");
        result.Should().ContainKey("Opmerkingen");
        result.Should().ContainKey(Terminologie.Huidig.InstructieTechniek);
        result.Should().ContainKey("Locatie Specificatie");
    }

    [Test]
    public void Captures_values_from_editable_fields()
    {
        var model = CreateModel();
        model.EditableTitle = "My Title";
        model.EditableElevatorPitch = "Pitch";
        model.EditableSamenvattingPlot = "Plot summary";
        model.EditableMeerInformatie = "Info";
        model.EditableOpmerkingen = "Notes";
        model.EditableInstructiePentagon = "Techniek";
        model.EditableLocatieEncounterSpecificatie = "Bos";

        var result = model.CaptureEditableTextFields();

        result["Titel"].Should().Be("My Title");
        result["Elevator Pitch"].Should().Be("Pitch");
        result["Samenvatting Plot"].Should().Be("Plot summary");
        result["Meer Informatie"].Should().Be("Info");
        result["Opmerkingen"].Should().Be("Notes");
        result[Terminologie.Huidig.InstructieTechniek].Should().Be("Techniek");
        result["Locatie Specificatie"].Should().Be("Bos");
    }

    [Test]
    public void Empty_quill_markup_normalizes_to_empty_string()
    {
        var model = CreateModel();
        model.EditableMeerInformatie = "<div class=\"ql-editor ql-blank\" contenteditable=\"true\"><p><br></p></div><div class=\"ql-tooltip ql-hidden\"><a class=\"ql-preview\" rel=\"noopener noreferrer\" target=\"_blank\" href=\"about:blank\"></a><input type=\"text\" data-formula=\"e=mc^2\" data-link=\"https://quilljs.com\" data-video=\"Embed URL\"><a class=\"ql-action\"></a><a class=\"ql-remove\"></a></div>";

        var result = model.CaptureEditableTextFields();

        TextNormalization.Normalize(result["Meer Informatie"] ?? string.Empty).Should().BeEmpty();
    }

    [Test]
    public void Null_fields_are_captured_as_null()
    {
        var model = CreateModel();

        var result = model.CaptureEditableTextFields();

        result["Titel"].Should().BeNull();
        result["Elevator Pitch"].Should().BeNull();
        result["Locatie Specificatie"].Should().BeNull();
    }
}
