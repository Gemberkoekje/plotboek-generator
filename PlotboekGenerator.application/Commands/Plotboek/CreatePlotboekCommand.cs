using PlotboekGenerator.infrastructure.Models;

namespace PlotboekGenerator.Application.Commands.Plotboek;

/// <summary>
/// Command to create a new plotboek.
/// </summary>
/// <param name="Year">The year of the plotboek.</param>
/// <param name="EventId">The id of the event's <see cref="PlotboekGenerator.core.Models.ReferenceData.EventConfig"/>.</param>
/// <param name="StartDag">The event's first day.</param>
/// <param name="EindDag">The event's last day.</param>
/// <param name="UpdatedBy">The user creating the plotboek.</param>
public sealed record CreatePlotboekCommand(Year Year, string EventId, Dag StartDag, Dag EindDag, PlotboekUser UpdatedBy) : ICommand<PlotboekId>;
