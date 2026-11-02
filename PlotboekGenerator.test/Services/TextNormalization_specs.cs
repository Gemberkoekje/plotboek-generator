using PlotboekGenerator.core;

namespace PlotboekGenerator.test.Services;

public sealed class TextNormalization_specs
{
    [Test]
    public void Normalizes_empty_quill_editor_markup_to_empty_string()
    {
        const string input = "<div class=\"ql-editor ql-blank\" contenteditable=\"true\"><p><br></p></div><div class=\"ql-tooltip ql-hidden\"><a class=\"ql-preview\" rel=\"noopener noreferrer\" target=\"_blank\" href=\"about:blank\"></a><input type=\"text\" data-formula=\"e=mc^2\" data-link=\"https://quilljs.com\" data-video=\"Embed URL\"><a class=\"ql-action\"></a><a class=\"ql-remove\"></a></div>";

        var result = TextNormalization.Normalize(input);

        result.Should().BeEmpty();
    }

    [Test]
    public void Keeps_non_empty_quill_editor_markup_content()
    {
        const string input = "<div class=\"ql-editor\" contenteditable=\"true\"><p>Inhoud</p></div><div class=\"ql-tooltip ql-hidden\"></div>";

        var result = TextNormalization.Normalize(input);

        result.Should().Be("<div class=\"ql-editor\" contenteditable=\"true\"><p>Inhoud</p></div>");
    }
}
