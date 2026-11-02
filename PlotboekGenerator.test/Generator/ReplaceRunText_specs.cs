using DocumentFormat.OpenXml.Wordprocessing;
using PlotboekGenerator.infrastructure.Generator;

namespace PlotboekGenerator.test.Generator;

/// <summary>
/// The "Kleding &amp; Schmink" cell separates its casting/afstemming/kleding blocks with a blank line,
/// which reaches the document as two consecutive <c>\n</c>. These specs pin down that the conversion to
/// OpenXML does not swallow them.
/// </summary>
public sealed class ReplaceRunText_specs
{
    [Test]
    public void Single_newline_becomes_one_break()
    {
        var run = new Run();

        DocxTemplateEngine.ReplaceRunText(run, "Guards: malienkolder\nEdelman: deftige kleding");

        run.Elements<Break>().Should().ContainSingle();
        run.Elements<Text>().Select(t => t.Text).Should().Equal("Guards: malienkolder", "Edelman: deftige kleding");
    }

    [Test]
    public void Blank_line_becomes_two_breaks()
    {
        var run = new Run();

        DocxTemplateEngine.ReplaceRunText(run, "Casting:\nGuards: goede roleplayer\n\nKleding & schmink:\nGuards: malienkolder");

        run.Elements<Break>().Should().HaveCount(4, "four newlines, two of which form the blank line");
        run.Elements<Text>().Should().HaveCount(5);
    }

    [Test]
    public void Blank_line_keeps_an_empty_text_between_the_breaks()
    {
        var run = new Run();

        DocxTemplateEngine.ReplaceRunText(run, "a\n\nb");

        run.ChildElements.Select(e => e is Break ? "<br>" : ((Text)e).Text)
            .Should().Equal("a", "<br>", string.Empty, "<br>", "b");
    }

    [Test]
    public void Empty_value_yields_a_single_empty_text()
    {
        var run = new Run();

        DocxTemplateEngine.ReplaceRunText(run, string.Empty);

        run.Elements<Break>().Should().BeEmpty();
        run.Elements<Text>().Should().ContainSingle().Which.Text.Should().BeEmpty();
    }
}
