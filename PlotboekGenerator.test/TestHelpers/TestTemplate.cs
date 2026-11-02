using PlotboekGenerator.Application.Ports;
using PlotboekGenerator.infrastructure.Generator;

namespace PlotboekGenerator.test.TestHelpers;

/// <summary>
/// The Word template the generator specs merge into: the one the host under test ships.
/// </summary>
internal static class TestTemplate
{
    /// <summary>The host's template.</summary>
    public static IDocumentTemplateProvider Provider { get; } = new EmbeddedResourceTemplateProvider(
        typeof(PlotboekGenerator.Host.DefaultProfile).Assembly,
        "PlotboekGenerator.Host.Templates.PlotboekTemplate.docx");
}
