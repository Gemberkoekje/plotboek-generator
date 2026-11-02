using System.Globalization;
using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging.Abstractions;
using PlotboekGenerator.Application.Ports;
using PlotboekGenerator.Host;
using PlotboekGenerator.infrastructure.Generator;
using PlotboekGenerator.WebApp;

namespace PlotboekGenerator.test.Generator;

/// <summary>
/// The generic host ships its Word template in Dutch and in English. The English one is the Dutch one
/// with its own words translated: it binds the same fields in the same places, and it exports the
/// example plotboek of its language.
/// </summary>
public sealed class ExampleTemplates_specs
{
    /// <summary>The template's own words that read the same in both languages.</summary>
    private static readonly string[] SameInBothLanguages = ["Icons", "Encounters", "Debrief", "# NPCs", "Character"];

    [TestCase("nl", "De Nevelkroon", "De bode uit Nevelmeer", "Naam encounter", "Encounter name")]
    [TestCase("en", "The Mist Crown", "The messenger from Mistmere", "Encounter name", "Naam encounter")]
    public async Task The_example_plotboek_exports_with_the_template_of_its_language(string language, string titel, string pagina, string label, string otherLanguageLabel)
    {
        var store = new TestDocumentStore();
        var events = new InMemoryEventStore();
        var seed = TestReferenceData.For(language);
        await new ReferenceDataSeedService(seed, store, NullLogger<ReferenceDataSeedService>.Instance).SeedAsync();
        await new DemoContentSeedService(
                Content(language), seed, store, new PlotboekRepository(events), new PlotboekPaginaRepository(events), NullLogger<DemoContentSeedService>.Instance)
            .SeedAsync();
        var plotboekId = store.GetAll<Plotboek.ExportData>().Single().PlotboekId;

        // The export speaks the deployment's default language, whoever asks for it.
        var result = await CultuurScope.InAsync(CultureInfo.GetCultureInfo(language), async () =>
        {
            var data = await new PlotboekExportBuilder(store).BuildAsync(plotboekId);
            data.IsValid.Should().BeTrue(string.Join(", ", data.Messages.Select(m => m.Message)));
            return await new DocumentGenerationService(Template(language)).GeneratePlotboekDocumentAsync(data.Value);
        });

        result.IsValid.Should().BeTrue(string.Join(", ", result.Messages.Select(m => m.Message)));
        using var stream = new MemoryStream(result.Value);
        using var document = WordprocessingDocument.Open(stream, false);
        var text = document.MainDocumentPart!.Document!.Body!.InnerText;
        text.Should().Contain(titel).And.Contain(pagina, "the plots are merged in")
            .And.Contain(label, "the labels are the template's own").And.NotContain(otherLanguageLabel);
    }

    [Test]
    public void The_English_template_binds_the_same_fields_as_the_Dutch_one()
    {
        using var dutch = Open(DefaultProfile.TemplateResource);
        using var english = Open(DefaultProfile.EnglishTemplateResource);

        ContentControls(english).Should().Equal(ContentControls(dutch), "the export finds a field by its tag, in either template");
    }

    [Test]
    public void Every_word_of_the_English_template_is_translated()
    {
        using var dutch = Open(DefaultProfile.TemplateResource);
        using var english = Open(DefaultProfile.EnglishTemplateResource);
        var dutchWords = OwnParagraphs(dutch).ToHashSet(StringComparer.Ordinal);
        dutchWords.Should().Contain(["Inhoudsopgave", "Naam encounter"], "the labels inside the repeated sections count too");

        OwnParagraphs(english).Where(dutchWords.Contains).Except(SameInBothLanguages)
            .Should().BeEmpty("a paragraph the English template shares with the Dutch one is one nobody translated");
    }

    [Test]
    public void The_English_template_is_as_valid_as_the_Dutch_one()
    {
        using var dutch = Open(DefaultProfile.TemplateResource);
        using var english = Open(DefaultProfile.EnglishTemplateResource);
        var validator = new OpenXmlValidator(FileFormatVersions.Microsoft365);

        validator.Validate(english).Select(error => error.Id).Distinct()
            .Should().BeSubsetOf(validator.Validate(dutch).Select(error => error.Id), "translating changes the words, not the markup");
    }

    /// <summary>The tag and alias of every content control, in document order.</summary>
    private static List<string> ContentControls(WordprocessingDocument document)
        => [.. Body(document).Descendants<SdtProperties>()
            .Select(properties => $"{properties.GetFirstChild<Tag>()?.Val?.Value}|{properties.GetFirstChild<SdtAlias>()?.Val?.Value}")];

    /// <summary>
    /// The text of every paragraph that is the template's own: what is not inside a field the export fills
    /// in (a content control with a tag). The labels inside a repeated section are the template's own.
    /// </summary>
    private static IEnumerable<string> OwnParagraphs(WordprocessingDocument document)
        => Body(document).Descendants<Paragraph>()
            .Select(paragraph => string.Concat(paragraph.Descendants<Text>().Where(IsOwn).Select(text => text.Text)))
            .Where(text => !string.IsNullOrWhiteSpace(text));

    private static Body Body(WordprocessingDocument document)
        => document.MainDocumentPart?.Document?.Body ?? throw new InvalidOperationException("The template has no body.");

    private static bool IsOwn(Text text)
    {
        var field = text.Ancestors().OfType<SdtElement>().FirstOrDefault();
        return string.IsNullOrEmpty(field?.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value);
    }

    private static WordprocessingDocument Open(string resource)
        => WordprocessingDocument.Open(typeof(DefaultProfile).Assembly.GetManifestResourceStream(resource)!, false);

    private static IDocumentTemplateProvider Template(string language)
        => new EmbeddedResourceTemplateProvider(
            typeof(DefaultProfile).Assembly,
            language == "en" ? DefaultProfile.EnglishTemplateResource : DefaultProfile.TemplateResource);

    private static IDemoContent Content(string language)
        => language == "en" ? new EnglishExampleDemoContent() : new ExampleDemoContent();
}
