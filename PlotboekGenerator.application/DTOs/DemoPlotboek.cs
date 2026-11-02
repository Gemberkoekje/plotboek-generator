namespace PlotboekGenerator.Application.DTOs;

/// <summary>A plotboek in a demo's example, written for the current year.</summary>
public sealed record DemoPlotboek
{
    /// <summary>The id of the event it is written for; its days come from that event.</summary>
    required public string EventId { get; init; }

    /// <summary>The title.</summary>
    required public string Titel { get; init; }

    /// <summary>The id of the spelleider who starts it.</summary>
    required public string AangemaaktDoorId { get; init; }

    /// <summary>The summary of the overall plot, as HTML.</summary>
    public string SamenvattingAlgemeenPlot { get; init; } = string.Empty;

    /// <summary>Whether the whole plotboek is marked fully done, which locks it for everyone but admins.</summary>
    public bool IsVolledigKlaar { get; init; }

    /// <summary>The paginas, in the order they are written.</summary>
    public IReadOnlyList<DemoPagina> Paginas { get; init; } = [];
}
