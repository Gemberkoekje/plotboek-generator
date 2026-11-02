namespace PlotboekGenerator.core.Models;

/// <summary>
/// A reusable attachment ("bijlage") defined once on a <see cref="Plotboek"/> and referenced by any
/// number of <see cref="PlotboekPagina"/> encounters. Rendered once in the plotboek's "Bijlagen"
/// appendix; each referencing encounter gets a page cross-reference to it.
/// </summary>
public sealed record BijlageData
{
    /// <summary>Stable identity, minted once when the bijlage is created and reused as the cross-reference anchor.</summary>
    public BijlageId Id { get; init; }

    /// <summary>Human title shown as the appendix heading and inside the "Voor {Titel} zie bijlage ..." reference.</summary>
    public string Titel { get; init; } = string.Empty;

    /// <summary>Sanitized rich-text (Quill) HTML content, same shape and handling as <see cref="PlotboekPagina.MeerInformatie"/>.</summary>
    public string Inhoud { get; init; } = string.Empty;

    /// <summary>Position within the plotboek's bijlagen list; drives appendix ordering.</summary>
    public int Order { get; init; }

    /// <summary>A bijlage is meaningful only once it has a title.</summary>
    public bool HasValue => !string.IsNullOrWhiteSpace(Titel);

    public static BijlageData Create(BijlageId id, string titel, string inhoud, int order)
    {
        return new BijlageData
        {
            Id = id,
            Titel = titel ?? string.Empty,
            Inhoud = inhoud ?? string.Empty,
            Order = order,
        };
    }
}
