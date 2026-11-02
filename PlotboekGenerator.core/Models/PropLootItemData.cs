namespace PlotboekGenerator.core.Models;

/// <summary>
/// Represents a single prop or loot item with a description and the SL responsible for providing it.
/// </summary>
public sealed record PropLootItemData
{
    /// <summary>
    /// Description of the prop or loot item.
    /// </summary>
    required public string Omschrijving { get; init; }

    /// <summary>
    /// The user ID of the SL responsible for providing this item on the terrain.
    /// </summary>
    public string VerantwoordelijkeSlId { get; init; }

    /// <summary>
    /// The display name of the SL responsible for providing this item.
    /// </summary>
    public string VerantwoordelijkeSlName { get; init; }

    /// <summary>
    /// Source type for the responsible person: "SL", "SpelerOfNpc" or "Anders".
    /// </summary>
    public string VerantwoordelijkeSource { get; init; }

    /// <summary>
    /// Free-text responsible person when source is not "SL".
    /// </summary>
    public string VerantwoordelijkeVrijeTekst { get; init; }

    public bool HasValue => !string.IsNullOrWhiteSpace(Omschrijving);

    public static PropLootItemData Empty => new ()
    {
        Omschrijving = string.Empty,
        VerantwoordelijkeSource = "SL",
    };

    public static PropLootItemData Create(
        string omschrijving,
        string verantwoordelijkeSlId = null,
        string verantwoordelijkeSlName = null,
        string verantwoordelijkeSource = "SL",
        string verantwoordelijkeVrijeTekst = null)
    {
        return new PropLootItemData
        {
            Omschrijving = omschrijving ?? string.Empty,
            VerantwoordelijkeSlId = verantwoordelijkeSlId,
            VerantwoordelijkeSlName = verantwoordelijkeSlName,
            VerantwoordelijkeSource = verantwoordelijkeSource,
            VerantwoordelijkeVrijeTekst = verantwoordelijkeVrijeTekst,
        };
    }

    /// <summary>
    /// Returns formatted string for DOCX export: "Omschrijving (SL Name)" or just "Omschrijving".
    /// </summary>
    public override string ToString()
    {
        if (!HasValue)
            return string.Empty;

        // Prefer SL name when source indicates SL and a name is present.
        if (string.Equals(VerantwoordelijkeSource, "SL", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(VerantwoordelijkeSlName))
        {
            return $"{Omschrijving} ({VerantwoordelijkeSlName})";
        }

        // Otherwise prefer free-text if provided.
        if (!string.IsNullOrWhiteSpace(VerantwoordelijkeVrijeTekst))
        {
            return $"{Omschrijving} ({VerantwoordelijkeVrijeTekst})";
        }

        return Omschrijving;
    }
}
