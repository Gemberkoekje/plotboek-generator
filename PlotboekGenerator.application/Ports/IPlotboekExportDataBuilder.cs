using PlotboekGenerator.core.Generator.Templates;

namespace PlotboekGenerator.Application.Ports;

/// <summary>
/// Builds the data model used for plotboek document generation.
/// </summary>
public interface IPlotboekExportDataBuilder
{
    /// <summary>
    /// Builds export data for a plotboek.
    /// </summary>
    /// <param name="plotboekId">The plotboek ID.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing export data when the plotboek exists.</returns>
    Task<Result<PlotboekTemplateData>> BuildAsync(PlotboekId plotboekId, CancellationToken cancellationToken = default);
}
