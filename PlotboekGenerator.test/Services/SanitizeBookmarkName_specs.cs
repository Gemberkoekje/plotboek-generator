namespace PlotboekGenerator.test.Services;

public sealed class SanitizeBookmarkName_specs
{
    [Test]
    public void Extra_info_bookmark_has_ei_prefix_and_underscores()
    {
        var id = PlotboekPaginaId.Next();
        var result = PlotboekExportBuilder.SanitizeBookmarkName(id);

        result.Should().StartWith("_ei_");
        result.Should().NotContain("-");
    }

    [Test]
    public void Page_bookmark_has_pg_prefix_and_underscores()
    {
        var id = PlotboekPaginaId.Next();
        var result = PlotboekExportBuilder.SanitizePageBookmarkName(id);

        result.Should().StartWith("_pg_");
        result.Should().NotContain("-");
    }

    [Test]
    public void Same_id_produces_consistent_bookmark()
    {
        var id = PlotboekPaginaId.Next();
        var first = PlotboekExportBuilder.SanitizeBookmarkName(id);
        var second = PlotboekExportBuilder.SanitizeBookmarkName(id);

        first.Should().Be(second);
    }

    [Test]
    public void Different_ids_produce_different_bookmarks()
    {
        var id1 = PlotboekPaginaId.Next();
        var id2 = PlotboekPaginaId.Next();

        PlotboekExportBuilder.SanitizeBookmarkName(id1)
            .Should().NotBe(PlotboekExportBuilder.SanitizeBookmarkName(id2));
    }
}

