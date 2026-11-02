namespace PlotboekGenerator.Application.Commands.Export;

/// <summary>
/// Command to export a plotboek as a DOCX document.
/// </summary>
/// <param name="PlotboekId">The ID of the plotboek to export.</param>
public sealed record ExportPlotboekCommand(PlotboekId PlotboekId) : ICommand<byte[]>;
