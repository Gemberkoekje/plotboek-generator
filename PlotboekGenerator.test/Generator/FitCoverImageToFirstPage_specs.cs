using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
using PlotboekGenerator.core.Generator.Templates;
using System.Collections.Immutable;
using System.IO;

namespace PlotboekGenerator.test.Generator;

/// <summary>
/// A plotboek title long enough to wrap onto many lines at the cover's 50pt font must not leave the
/// cover picture at its full, untouched size - that reproduces the exact "title spills onto page
/// two" bug this shrink step exists to prevent.
/// </summary>
public sealed class FitCoverImageToFirstPage_specs
{
    [TestCase(20)]
    [TestCase(85)]
    [TestCase(120)]
    [TestCase(400)]
    public async Task Long_title_always_shrinks_the_cover_image_below_its_template_size(int titleLength)
    {
        var model = BuildModel(new string('E', titleLength));

        var result = await new DocumentGenerationService(TestTemplate.Provider).GeneratePlotboekDocumentAsync(model);
        result.IsValid.Should().BeTrue();

        var templateHeight = ReadCoverImageHeight(await ReadTemplateBytesAsync());
        var generatedHeight = ReadCoverImageHeight(result.Value);

        generatedHeight.Should().BeLessThan(templateHeight, "the cover picture must shrink, never stay at its full template size, once the title overflows the page");
    }

    [Test]
    public async Task Title_far_too_long_to_fully_fit_still_shrinks_the_image_to_the_floor()
    {
        var model = BuildModel(new string('E', 2000));

        var result = await new DocumentGenerationService(TestTemplate.Provider).GeneratePlotboekDocumentAsync(model);
        result.IsValid.Should().BeTrue();

        var templateHeight = ReadCoverImageHeight(await ReadTemplateBytesAsync());
        var generatedHeight = ReadCoverImageHeight(result.Value);

        // Even an extreme title still shrinks the picture down to (roughly) its minimum floor,
        // rather than giving up and leaving it untouched.
        generatedHeight.Should().BeLessThan((long)(templateHeight * 0.10));
    }

    private static async Task<byte[]> ReadTemplateBytesAsync()
    {
        using var stream = TestTemplate.Provider.OpenTemplate();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }

    private static long ReadCoverImageHeight(byte[] docxBytes)
    {
        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var extent = doc.MainDocumentPart!.Document!.Body!.Descendants<Extent>().First();
        return extent.Cy!.Value;
    }

    private static PlotboekTemplateData BuildModel(string title)
        => new PlotboekTemplateData
        {
            Naam = title,
            SamenvattingAlgemeenPlot = string.Empty,
            Tijdschemas = ImmutableList<TijdschemasData>.Empty,
            Days = ImmutableList<DayData>.Empty,
            ExtraInfo = ImmutableList<ExtraInfoData>.Empty,
            Bijlagen = [],
            AslDomeinen = ImmutableList<DomeinData>.Empty,
            GslDomeinen = ImmutableList<DomeinData>.Empty,
            EvergreenPages = ImmutableList<PageData>.Empty,
            Namen = ImmutableList<NaamData>.Empty,
            Werelden = ImmutableList<NaamData>.Empty,
            EvergreenBijlageNummer = "1",
            NamenBijlageNummer = "2",
        };
}
