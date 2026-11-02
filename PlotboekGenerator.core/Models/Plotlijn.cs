using System.Collections.Immutable;
using System.Globalization;
using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Models;

/// <summary>
/// A plotlijn is a collection of related plot pages within the same domain.
/// It has a computed title based on the titles of its member plots.
/// Stored directly as a Marten document.
/// </summary>
public sealed record Plotlijn
{
    /// <summary>
    /// String ID for Marten document store.
    /// </summary>
    public string Id { get => PlotlijnId.ToString(); }

    /// <summary>
    /// Queryable plotboek ID for database queries.
    /// </summary>
    public string PlotboekIdQueryable { get => PlotboekId.ToString(); }

    public PlotlijnId PlotlijnId { get; init; }

    public PlotboekId PlotboekId { get; init; }

    public Guid DomainId { get; init; }

    public string DomainShorthand { get; init; }

    public ImmutableList<string> PaginaTitels { get; init; } = [];

    /// <summary>
    /// Computed display title for the plotlijn.
    /// For 1-3 plots: "Plot1; Plot2; Plot3"
    /// For 4+ plots: "Plot1 (+N plots)".
    /// </summary>
    public string Titel
    {
        get
        {
            if (PaginaTitels.Count == 0)
            {
                return string.Empty;
            }
            if (PaginaTitels.Count <= 3)
            {
                return string.Join("; ", PaginaTitels);
            }
            return string.Format(CultureInfo.CurrentCulture, CoreTekst.PlotlijnTitelMeer, PaginaTitels[0], PaginaTitels.Count - 1);
        }
    }
}
