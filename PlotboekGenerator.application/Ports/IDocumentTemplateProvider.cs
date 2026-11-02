using System.IO;

namespace PlotboekGenerator.Application.Ports;

/// <summary>
/// Supplies the Word template a plotboek export is merged into, so every deployment brings its own
/// look. The template's content controls are tagged with the property names of
/// <see cref="PlotboekGenerator.core.Generator.Templates.PlotboekTemplateData"/>.
/// </summary>
public interface IDocumentTemplateProvider
{
    /// <summary>Opens a fresh stream over the template.</summary>
    /// <returns>A readable stream the caller disposes.</returns>
    Stream OpenTemplate();
}
