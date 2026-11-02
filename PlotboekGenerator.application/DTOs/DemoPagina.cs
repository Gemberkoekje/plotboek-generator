namespace PlotboekGenerator.Application.DTOs;

/// <summary>A pagina (one encounter) in a demo's example.</summary>
public sealed record DemoPagina
{
    /// <summary>
    /// What the pagina says. The writer fills in what the example cannot know: the domain from
    /// <see cref="DomainShorthand"/>, and the spelleiders' names from their ids.
    /// </summary>
    required public PlotboekPagina.Data Data { get; init; }

    /// <summary>The shorthand of the domain the encounter belongs to; empty for none.</summary>
    public string DomainShorthand { get; init; } = string.Empty;

    /// <summary>A key shared by the paginas of one plotlijn; empty for a self-contained plot.</summary>
    public string Plotlijn { get; init; } = string.Empty;

    /// <summary>The feedback on the pagina, in the order it was given.</summary>
    public IReadOnlyList<DemoFeedback> Feedback { get; init; } = [];
}
