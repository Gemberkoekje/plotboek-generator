namespace PlotboekGenerator.test.Pages;

public sealed class PlotboekModel_ValidateEditableBijlagen_specs
{
    private static PlotboekModel CreateModel()
        => new (null!, null!, null!, null!, null!, null!, null!, null!, null!);

    [Test]
    public void Accepts_an_empty_bijlagen_list()
    {
        var _sut = CreateModel();

        var result = _sut.ValidateEditableBijlagen();

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Accepts_rows_that_have_both_a_name_and_content()
    {
        var _sut = CreateModel();
        _sut.EditableBijlagen =
        [
            new PlotboekModel.BijlageInput { Naam = "Prijslijst", InhoudHtml = "<p>bier 2 kopermunten</p>" },
        ];

        var result = _sut.ValidateEditableBijlagen();

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Accepts_a_named_row_without_content()
    {
        var _sut = CreateModel();
        _sut.EditableBijlagen =
        [
            new PlotboekModel.BijlageInput { Naam = "Nog in te vullen", InhoudHtml = string.Empty },
        ];

        var result = _sut.ValidateEditableBijlagen();

        result.IsValid.Should().BeTrue("a bijlage may be named first and filled in later");
    }

    [Test]
    public void Accepts_a_row_that_is_entirely_empty()
    {
        var _sut = CreateModel();
        _sut.EditableBijlagen =
        [
            new PlotboekModel.BijlageInput { Naam = "  ", InhoudHtml = "<p><br></p>" },
        ];

        var result = _sut.ValidateEditableBijlagen();

        result.IsValid.Should().BeTrue("an added-but-untouched row carries nothing worth keeping");
    }

    [Test]
    public void Rejects_a_row_with_content_but_no_name()
    {
        var _sut = CreateModel();
        _sut.EditableBijlagen =
        [
            new PlotboekModel.BijlageInput { Naam = string.Empty, InhoudHtml = "<p>getypte tekst</p>" },
        ];

        var result = _sut.ValidateEditableBijlagen();

        result.IsValid.Should().BeFalse();
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("\t")]
    public void Treats_a_blank_name_as_no_name(string naam)
    {
        var _sut = CreateModel();
        _sut.EditableBijlagen =
        [
            new PlotboekModel.BijlageInput { Naam = naam, InhoudHtml = "<p>getypte tekst</p>" },
        ];

        var result = _sut.ValidateEditableBijlagen();

        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Names_the_offending_row_by_its_position_in_the_form()
    {
        var _sut = CreateModel();
        _sut.EditableBijlagen =
        [
            new PlotboekModel.BijlageInput { Naam = "Prijslijst", InhoudHtml = "<p>a</p>" },
            new PlotboekModel.BijlageInput { Naam = string.Empty, InhoudHtml = "<p>b</p>" },
        ];

        var result = _sut.ValidateEditableBijlagen();

        result.Messages.Should().ContainSingle();
        result.Messages.Single().Message.Should().StartWith("Bijlage 2 ", "the user has no other way to tell which row is meant");
    }

    [Test]
    public void Lists_every_offending_row_in_one_message()
    {
        var _sut = CreateModel();
        _sut.EditableBijlagen =
        [
            new PlotboekModel.BijlageInput { Naam = "Prijslijst", InhoudHtml = "<p>a</p>" },
            new PlotboekModel.BijlageInput { Naam = string.Empty, InhoudHtml = "<p>b</p>" },
            new PlotboekModel.BijlageInput { Naam = "  ", InhoudHtml = "<p>c</p>" },
            new PlotboekModel.BijlageInput { Naam = null, InhoudHtml = "<p>d</p>" },
        ];

        var result = _sut.ValidateEditableBijlagen();

        result.Messages.Should().ContainSingle();
        result.Messages.Single().Message.Should().StartWith("Bijlagen 2, 3 en 4 ");
    }

    [TestCase("<p>tekst</p>", true, TestName = "Typed_text_counts_as_content")]
    [TestCase("<p><strong>vet</strong></p>", true, TestName = "Formatted_text_counts_as_content")]
    [TestCase("<p><img src=\"data:image/png;base64,iVBOR\"></p>", true, TestName = "An_image_counts_as_content")]
    [TestCase("<table><tr><td></td></tr></table>", true, TestName = "A_table_counts_as_content")]
    [TestCase("<p>&nbsp;</p>", false, TestName = "A_non_breaking_space_is_not_content")]
    [TestCase("<p><br></p>", false, TestName = "An_untouched_editor_is_not_content")]
    [TestCase("<p></p>", false, TestName = "An_empty_paragraph_is_not_content")]
    [TestCase("", false, TestName = "An_empty_string_is_not_content")]
    [TestCase("   ", false, TestName = "Whitespace_is_not_content")]
    public void Recognizes_which_rich_text_actually_holds_something(string inhoudHtml, bool expected)
    {
        PlotboekModel.HasBijlageInhoud(inhoudHtml).Should().Be(expected);
    }

    [Test]
    public void Empty_quill_wrapper_markup_is_not_content()
    {
        const string emptyQuillMarkup = "<div class=\"ql-editor ql-blank\" contenteditable=\"true\"><p><br></p></div><div class=\"ql-tooltip ql-hidden\"><a class=\"ql-preview\" rel=\"noopener noreferrer\" target=\"_blank\" href=\"about:blank\"></a><input type=\"text\" data-formula=\"e=mc^2\" data-link=\"https://quilljs.com\" data-video=\"Embed URL\"><a class=\"ql-action\"></a><a class=\"ql-remove\"></a></div>";

        PlotboekModel.HasBijlageInhoud(emptyQuillMarkup).Should().BeFalse();
    }

    [Test]
    public void A_null_inhoud_is_not_content()
    {
        PlotboekModel.HasBijlageInhoud(null!).Should().BeFalse();
    }
}
