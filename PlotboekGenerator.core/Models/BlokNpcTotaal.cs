namespace PlotboekGenerator.core.Models;

/// <summary>
/// Represents NPC totals for a specific time block, including overflow from previous blocks.
/// </summary>
public sealed record BlokNpcTotaal
{
    /// <summary>
    /// The day this block is on.
    /// </summary>
    required public Dag Dag { get; init; }

    /// <summary>
    /// The time block.
    /// </summary>
    required public string StarttijdId { get; init; }

    /// <summary>
    /// Whether any encounter in this block requires ALL NPCs.
    /// </summary>
    public bool HeeftAlleNpcs { get; init; }

    /// <summary>
    /// Minimum NPCs needed for encounters starting in this block.
    /// </summary>
    public int DirectMinimum { get; init; }

    /// <summary>
    /// Maximum NPCs needed for encounters starting in this block.
    /// </summary>
    public int? DirectMaximum { get; init; }

    /// <summary>
    /// NPCs that overflow from previous blocks (multi-block encounters).
    /// </summary>
    public IReadOnlyList<OverflowNpcs> OverflowVanVorigeBlokken { get; init; } = [];

    /// <summary>
    /// Total minimum including overflow.
    /// </summary>
    public int TotaalMinimum => DirectMinimum + OverflowVanVorigeBlokken.Sum(o => o.Minimum);

    /// <summary>
    /// Total maximum including overflow (null if any component has no maximum).
    /// </summary>
    public int? TotaalMaximum => DirectMaximum.HasValue && OverflowMaximum.HasValue
        ? DirectMaximum.Value + OverflowMaximum.Value
        : null;

    public bool HasValue => HeeftAlleNpcs || DirectMinimum > 0 || DirectMaximum.HasValue || OverflowVanVorigeBlokken.Count > 0;

    /// <summary>
    /// Format for display in the UI.
    /// </summary>
    public string ToDisplayString()
    {
        if (!HasValue)
            return string.Empty;

        if (HeeftAlleNpcs)
            return "ALLE NPCs";

        var parts = new List<string>();

        // Main total
        if (TotaalMinimum > 0 || TotaalMaximum.HasValue)
        {
            var totalStr = TotaalMaximum.HasValue && TotaalMinimum != TotaalMaximum
                ? $"{TotaalMinimum}-{TotaalMaximum}"
                : $"{TotaalMinimum}";
            parts.Add($"{totalStr} NPCs");
        }

        // Add overflow details if any
        if (OverflowVanVorigeBlokken.Count > 0)
        {
            var overflowParts = OverflowVanVorigeBlokken
                .Select(o =>
                {
                    var countStr = o.Maximum.HasValue && o.Minimum != o.Maximum
                        ? $"{o.Minimum}-{o.Maximum}"
                        : $"{o.Minimum}";
                    return $"{countStr} van {o.VanStarttijdId}";
                });
            parts.Add($"(incl. {string.Join(", ", overflowParts)})");
        }

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Short format for compact display (total only).
    /// </summary>
    public string ToShortString()
    {
        if (!HasValue)
            return string.Empty;

        if (HeeftAlleNpcs)
            return "ALLE";

        if (TotaalMaximum.HasValue && TotaalMinimum != TotaalMaximum)
            return $"{TotaalMinimum}-{TotaalMaximum}";

        return TotaalMinimum > 0 ? $"{TotaalMinimum}" : string.Empty;
    }

    /// <summary>
    /// Gets the overflow summary for display (e.g., "+1-3 doorlopend").
    /// </summary>
    public string GetOverflowString()
    {
        if (OverflowVanVorigeBlokken.Count == 0)
            return string.Empty;

        var totalOverflowMin = OverflowVanVorigeBlokken.Sum(o => o.Minimum);
        var totalOverflowMax = OverflowVanVorigeBlokken.All(o => o.Maximum.HasValue)
            ? OverflowVanVorigeBlokken.Sum(o => o.Maximum!.Value)
            : (int?)null;

        if (totalOverflowMax.HasValue && totalOverflowMin != totalOverflowMax)
            return $"+{totalOverflowMin}-{totalOverflowMax}";

        return $"+{totalOverflowMin}";
    }

    /// <summary>
    /// Whether this block has overflow NPCs from previous blocks.
    /// </summary>
    public bool HasOverflow => OverflowVanVorigeBlokken.Count > 0;

    /// <summary>
    /// Berekent de CSS badge-klasse en het pijlsymbool voor dit blok op basis van verwacht aantal NPCs.
    /// Geeft (string.Empty, string.Empty) terug als verwacht 0 is (geen verwachting ingevuld).
    /// </summary>
    public (string CssClass, string Arrow) GetNpcStatus(int verwacht)
    {
        if (verwacht <= 0)
            return (string.Empty, string.Empty);

        if (HeeftAlleNpcs)
            return ("text-bg-success", string.Empty);

        // Als verwacht binnen het [min, max] bereik valt → altijd groen
        if (TotaalMaximum.HasValue && verwacht >= TotaalMinimum && verwacht <= TotaalMaximum.Value)
            return ("text-bg-success", string.Empty);

        // Bepaal afstand tot de dichtstbijzijnde grens en richting
        int afstand;
        string arrow;
        var isAboveMax = false;
        if (verwacht < TotaalMinimum)
        {
            // Minder NPCs beschikbaar dan minimaal nodig
            afstand = TotaalMinimum - verwacht;
            arrow = "▲";
        }
        else if (TotaalMaximum.HasValue)
        {
            // Meer NPCs beschikbaar dan maximaal nodig (verwacht > TotaalMaximum)
            afstand = verwacht - TotaalMaximum.Value;
            arrow = "▼";
            isAboveMax = true;
        }
        else
        {
            // Geen maximum bekend; verwacht >= TotaalMinimum — meet surplus boven minimum
            afstand = verwacht - TotaalMinimum;
            arrow = "▼";
        }

        // Use different thresholds depending on whether we're below minimum or above maximum.
        // If expected is below the minimum or above the maximum, do not show green;
        // small deviations will show yellow based on a baseline threshold.
        int groenDrempel;
        int geelDrempel;
        var isBelowMin = verwacht < TotaalMinimum;
        if (isAboveMax || isBelowMin)
        {
            // never green when we're outside the configured range
            groenDrempel = 0;
            // baseline yellow threshold
            geelDrempel = Math.Max(3, verwacht / 4);
        }
        else
        {
            groenDrempel = Math.Max(1, verwacht / 10);
            geelDrempel = Math.Max(3, verwacht / 4);
        }

        string cssClass;
        if (afstand <= groenDrempel)
        {
            cssClass = "text-bg-success";
            arrow = string.Empty;
        }
        else if (afstand <= geelDrempel)
        {
            cssClass = "text-bg-warning";
        }
        else
        {
            cssClass = "text-bg-danger";
        }

        return (cssClass, arrow);
    }

    /// <summary>
    /// Total minimum from overflow only.
    /// </summary>
    public int OverflowMinimum => OverflowVanVorigeBlokken.Sum(o => o.Minimum);

    /// <summary>
    /// Total maximum from overflow only.
    /// </summary>
    public int? OverflowMaximum => OverflowVanVorigeBlokken.All(o => o.Maximum.HasValue)
        ? OverflowVanVorigeBlokken.Sum(o => o.Maximum!.Value)
        : null;
}

/// <summary>
/// Represents NPCs that overflow from a previous block into the current block.
/// </summary>
public sealed record OverflowNpcs
{
    /// <summary>
    /// The starttijd ID where this encounter started.
    /// </summary>
    required public string VanStarttijdId { get; init; }

    /// <summary>
    /// Minimum NPCs from the overflowing encounter.
    /// </summary>
    public int Minimum { get; init; }

    /// <summary>
    /// Maximum NPCs from the overflowing encounter.
    /// </summary>
    public int? Maximum { get; init; }

    /// <summary>
    /// Whether this overflow is from an "ALL NPCs" encounter.
    /// </summary>
    public bool IsAlleNpcs { get; init; }
}
