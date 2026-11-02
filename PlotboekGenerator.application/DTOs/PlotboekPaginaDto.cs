namespace PlotboekGenerator.Application.DTOs;

/// <summary>
/// Data transfer object for plotboek pagina (page) read operations.
/// </summary>
/// <param name="Id">The plotboek pagina ID.</param>
/// <param name="PlotboekId">The ID of the parent plotboek.</param>
public sealed record PlotboekPaginaDto(PlotboekId Id, PlotboekId PlotboekId);
