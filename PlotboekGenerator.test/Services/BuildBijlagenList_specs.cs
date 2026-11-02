using PlotboekGenerator.core.Generator.Templates;

namespace PlotboekGenerator.test.Services;

public sealed class BuildBijlagenList_specs
{
    [Test]
    public void Returns_empty_when_no_bijlagen()
    {
        var result = PlotboekExportBuilder.BuildBijlagenList([]);

        result.Should().BeEmpty();
    }

    [Test]
    public void Emits_every_authored_bijlage_even_when_unreferenced()
    {
        // BuildBijlagenList takes only the definitions and emits all of them: the "general-explanation
        // bijlage with zero references still renders in the appendix" decision.
        var bijlagen = new List<BijlageData>
        {
            BijlageData.Create(BijlageId.Next(), "Prijslijst", "<p>a</p>", 0),
            BijlageData.Create(BijlageId.Next(), "Statblock", "<p>b</p>", 1),
        };

        var result = PlotboekExportBuilder.BuildBijlagenList(bijlagen);

        result.Should().HaveCount(2);
    }

    [Test]
    public void Drops_bijlagen_without_a_title()
    {
        var bijlagen = new List<BijlageData>
        {
            BijlageData.Create(BijlageId.Next(), "   ", "<p>a</p>", 0),
            BijlageData.Create(BijlageId.Next(), "Statblock", "<p>b</p>", 1),
        };

        var result = PlotboekExportBuilder.BuildBijlagenList(bijlagen);

        result.Should().ContainSingle();
        result[0].Titel.Should().Be("Statblock");
    }

    [Test]
    public void Orders_by_order_then_title()
    {
        var bijlagen = new List<BijlageData>
        {
            BijlageData.Create(BijlageId.Next(), "Zebra", "<p>z</p>", 1),
            BijlageData.Create(BijlageId.Next(), "Appel", "<p>a</p>", 0),
        };

        var result = PlotboekExportBuilder.BuildBijlagenList(bijlagen);

        result[0].Titel.Should().Be("Appel");
        result[1].Titel.Should().Be("Zebra");
    }

    [Test]
    public void Numbers_bijlagen_one_based_by_order()
    {
        var bijlagen = new List<BijlageData>
        {
            BijlageData.Create(BijlageId.Next(), "Eerste", "<p>a</p>", 0),
            BijlageData.Create(BijlageId.Next(), "Tweede", "<p>b</p>", 1),
        };

        var result = PlotboekExportBuilder.BuildBijlagenList(bijlagen);

        result[0].Nummer.Should().Be("1");
        result[1].Nummer.Should().Be("2");
    }

    [Test]
    public void Empty_content_becomes_a_blank_paragraph_not_a_leaked_placeholder()
    {
        var bijlagen = new List<BijlageData> { BijlageData.Create(BijlageId.Next(), "Alleen titel", "  ", 0) };

        var result = PlotboekExportBuilder.BuildBijlagenList(bijlagen);

        result[0].Inhoud.Should().Be("<p></p>");
    }

    [Test]
    public void Sets_bookmark_name_from_id()
    {
        var id = BijlageId.Next();
        var bijlagen = new List<BijlageData> { BijlageData.Create(id, "Prijslijst", "<p>a</p>", 0) };

        var result = PlotboekExportBuilder.BuildBijlagenList(bijlagen);

        result[0].BookmarkName.Should().Be(PlotboekExportBuilder.SanitizeBijlageBookmarkName(id));
        result[0].BookmarkName.Should().StartWith("_bl_");
    }

    [Test]
    public void Carries_the_content()
    {
        var bijlagen = new List<BijlageData> { BijlageData.Create(BijlageId.Next(), "Prijslijst", "<p>Brazen Bull</p>", 0) };

        var result = PlotboekExportBuilder.BuildBijlagenList(bijlagen);

        result[0].Inhoud.Should().Be("<p>Brazen Bull</p>");
    }
}
