using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PlotboekGenerator.core.Generator.Templates;
using System.Collections.Immutable;
using System.IO;

namespace PlotboekGenerator.test.Generator;

/// <summary>
/// End-to-end coverage: a Quill table stored in a MeerInformatie field must
/// survive the whole template merge (SDT injection, unwrap, save) as a real
/// Word table rather than being dumped as literal HTML text.
/// </summary>
public sealed class DocumentGeneration_Table_specs
{
    private const string QuillTableHtml =
        "<table style=\"border: 1px solid #000;\"><tbody>" +
        "<tr><td data-row=\"1\"></td><td data-row=\"1\">HP</td></tr>" +
        "<tr><td data-row=\"2\">ijsheksen</td><td data-row=\"2\" class=\"ql-align-right\">8</td></tr>" +
        "</tbody></table>";

    [Test]
    public async Task Renders_meer_informatie_table_as_a_real_word_table()
    {
        var result = await new DocumentGenerationService(TestTemplate.Provider)
            .GeneratePlotboekDocumentAsync(BuildModel(QuillTableHtml));

        result.IsValid.Should().BeTrue();

        using var ms = new MemoryStream(result.Value);
        using var doc = WordprocessingDocument.Open(ms, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        // The raw HTML must not leak into the document as literal text.
        body.InnerText.Should().NotContain("data-row");
        body.InnerText.Should().NotContain("<td");

        // The table content is present as actual table cells.
        var cellTexts = body.Descendants<TableCell>().Select(c => c.InnerText).ToList();
        cellTexts.Should().Contain("ijsheksen");
        cellTexts.Should().Contain("HP");
    }

    private static PlotboekTemplateData BuildModel(string meerInformatie)
        => new()
        {
            Naam = "Test Plotboek",
            SamenvattingAlgemeenPlot = string.Empty,
            Tijdschemas = ImmutableList<TijdschemasData>.Empty,
            Days = ImmutableList<DayData>.Empty,
            ExtraInfo = ImmutableList.Create(new ExtraInfoData
            {
                NaamEncounter = "Stem ondood",
                MeerInformatie = meerInformatie,
            }),
            AslDomeinen = ImmutableList<DomeinData>.Empty,
            GslDomeinen = ImmutableList<DomeinData>.Empty,
            EvergreenPages = ImmutableList<PageData>.Empty,
            Namen = ImmutableList<NaamData>.Empty,
            Werelden = ImmutableList<NaamData>.Empty,
        };
}
