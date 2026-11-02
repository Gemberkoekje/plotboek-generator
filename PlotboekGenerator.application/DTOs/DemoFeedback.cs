namespace PlotboekGenerator.Application.DTOs;

/// <summary>A feedback item on a pagina in a demo's example.</summary>
public sealed record DemoFeedback
{
    /// <summary>The id of the spelleider who gave it.</summary>
    required public string GegevenDoorId { get; init; }

    /// <summary>The feedback.</summary>
    required public string Tekst { get; init; }

    /// <summary>The answer of the pagina's responsible spelleider; empty when it is still open.</summary>
    public string Reactie { get; init; } = string.Empty;

    /// <summary>Whether the giver signed it off after the answer.</summary>
    public bool IsAfgehandeld { get; init; }
}
