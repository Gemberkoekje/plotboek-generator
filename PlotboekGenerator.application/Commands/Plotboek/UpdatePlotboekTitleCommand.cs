using PlotboekGenerator.infrastructure.Models;

namespace PlotboekGenerator.Application.Commands.Plotboek;

/// <summary>
/// Command to update a plotboek title.
/// </summary>
/// <param name="PlotboekId">The ID of the plotboek to update.</param>
/// <param name="Title">The new title.</param>
/// <param name="UpdatedBy">The user performing the update.</param>
public sealed record UpdatePlotboekTitleCommand(PlotboekId PlotboekId, string Title, PlotboekUser UpdatedBy) : ICommand;
