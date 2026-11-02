#pragma warning disable S3220 // All OpenXML composite-element constructors have both params and IEnumerable overloads

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Office2013.Word;
using DocumentFormat.OpenXml.Wordprocessing;
using PlotboekGenerator.infrastructure.Generator;

namespace PlotboekGenerator.test.Generator;

public sealed class ExpandRepeater_specs
{
    [Test]
    public void Removes_repeater_when_path_resolves_to_nothing()
    {
        var body = new Body();
        var repeater = CreateRepeaterSdt("Items");
        body.AppendChild(repeater);

        DocxTemplateEngine.ExpandRepeater(repeater, new { }, "Items");

        body.ChildElements.Should().BeEmpty();
    }

    [Test]
    public void Removes_repeater_when_items_collection_is_empty()
    {
        var body = new Body();
        var repeater = CreateRepeaterSdt("Items");
        body.AppendChild(repeater);

        DocxTemplateEngine.ExpandRepeater(repeater, new { Items = Array.Empty<object>() }, "Items");

        body.ChildElements.Should().BeEmpty();
    }

    [Test]
    public void Expands_one_clone_per_item()
    {
        var body = new Body();
        var repeater = CreateRepeaterSdt("Items");
        body.AppendChild(repeater);

        var model = new { Items = new[] { new { Name = "A" }, new { Name = "B" }, new { Name = "C" } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        // Original repeater removed, 3 clones inserted as direct children
        body.ChildElements.OfType<SdtBlock>().Should().HaveCount(3);
    }

    [Test]
    public void Clones_are_marked_as_Scope_not_repeater()
    {
        var body = new Body();
        var repeater = CreateRepeaterSdt("Items");
        body.AppendChild(repeater);

        var model = new { Items = new[] { new { Name = "X" } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        var clone = body.ChildElements.OfType<SdtBlock>().Single();
        var tag = clone.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value;
        tag.Should().Be("Scope");
    }

    [Test]
    public void Binds_text_values_from_item_context()
    {
        var body = new Body();
        var repeater = CreateRepeaterWithTextField("Items", "Name");
        body.AppendChild(repeater);

        var model = new { Items = new[] { new { Name = "Hello" } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        // The field SDT is unwrapped during binding, leaving the text directly in the paragraph
        var texts = body.Descendants<Text>().Select(t => t.Text).ToList();
        texts.Should().Contain(t => t.Contains("Hello") || t.Contains("template"));
    }

    [Test]
    public void Inserts_bookmark_when_item_has_BookmarkName()
    {
        var body = new Body();
        var repeater = CreateRepeaterSdt("Items");
        body.AppendChild(repeater);

        var model = new { Items = new[] { new { BookmarkName = "_bm_test" } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        body.Descendants<BookmarkStart>().Should().ContainSingle();
        body.Descendants<BookmarkStart>().Single().Name!.Value.Should().Be("_bm_test");
        body.Descendants<BookmarkEnd>().Should().ContainSingle();
    }

    [Test]
    public void Does_not_insert_bookmark_when_BookmarkName_is_empty()
    {
        var body = new Body();
        var repeater = CreateRepeaterSdt("Items");
        body.AppendChild(repeater);

        var model = new { Items = new[] { new { BookmarkName = string.Empty } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        body.Descendants<BookmarkStart>().Should().BeEmpty();
    }

    /// <summary>
    /// Creates a minimal SdtBlock that is recognized as a repeater.
    /// </summary>
    private static SdtBlock CreateRepeaterSdt(string tag) =>
        new (
            new SdtProperties(
                new Tag { Val = tag },
                new SdtRepeatedSection()),
            new SdtContentBlock(
                new SdtBlock(
                    new SdtProperties(new SdtRepeatedSectionItem()),
                    new SdtContentBlock(
                        new Paragraph(new Run(new Text("template")))))));

    /// <summary>
    /// Creates a repeater SDT that contains a text field tagged with the given field name.
    /// </summary>
    private static SdtBlock CreateRepeaterWithTextField(string repeaterTag, string fieldTag) =>
        new (
            new SdtProperties(
                new Tag { Val = repeaterTag },
                new SdtRepeatedSection()),
            new SdtContentBlock(
                new SdtBlock(
                    new SdtProperties(new SdtRepeatedSectionItem()),
                    new SdtContentBlock(
                        new SdtBlock(
                            new SdtProperties(new Tag { Val = fieldTag }),
                            new SdtContentBlock(
                                new Paragraph(new Run(new Text("placeholder")))))))));
}


