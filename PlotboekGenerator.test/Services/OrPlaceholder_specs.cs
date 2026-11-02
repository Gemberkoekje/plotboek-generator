using PlotboekGenerator.core.Generator.Templates;

namespace PlotboekGenerator.test.Services;

public sealed class OrPlaceholder_specs
{
    [Test]
    public void Returns_value_when_non_empty()
    {
        PlotboekExportBuilder.OrPlaceholder("hello").Should().Be("hello");
    }

    [Test]
    public void Returns_placeholder_for_null()
    {
        PlotboekExportBuilder.OrPlaceholder(null!).Should().Be(PageData.NogNietIngevuld);
    }

    [Test]
    public void Returns_placeholder_for_empty_string()
    {
        PlotboekExportBuilder.OrPlaceholder(string.Empty).Should().Be(PageData.NogNietIngevuld);
    }

    [Test]
    public void Returns_placeholder_for_whitespace()
    {
        PlotboekExportBuilder.OrPlaceholder("   ").Should().Be(PageData.NogNietIngevuld);
    }
}

