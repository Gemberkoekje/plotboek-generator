namespace PlotboekGenerator.core.Models;

/// <summary>
/// Represents the location of an encounter with an optional specification field.
/// Groeperingsveld always needs the free-text <see cref="Specificatie"/>. Questveld is picked from the
/// curated questlocatie list (<see cref="LocatieId"/>); only the "Anders…" escape hatch falls back to
/// free text, and then the specification is mandatory again.
/// </summary>
public readonly record struct LocatieEncounterData
{
    public LocatieEncounter Locatie { get; init; }

    /// <summary>
    /// The chosen <see cref="ReferenceData.QuestLocatieConfig"/> id. Empty means free text
    /// ("Anders…") or no questveld at all. The display name is deliberately not resolved here:
    /// <c>core</c> does not know the reference data, so the UI and the export builder resolve it
    /// and a rename therefore shows up everywhere.
    /// </summary>
    public string LocatieId { get; init; }

    public string Specificatie { get; init; }

    public bool HasValue => Locatie != LocatieEncounter.None;

    /// <summary>Whether a curated questlocatie was picked (as opposed to free text).</summary>
    public bool HasQuestLocatieKeuze => Locatie == LocatieEncounter.Questveld && !string.IsNullOrWhiteSpace(LocatieId);

    /// <summary>
    /// Groeperingsveld always needs a specification; Questveld only when no questlocatie was picked.
    /// </summary>
    public bool RequiresSpecificatie => Locatie == LocatieEncounter.Groeperingsveld
        || (Locatie == LocatieEncounter.Questveld && !HasQuestLocatieKeuze);

    public static LocatieEncounterData Empty => new ()
    {
        Locatie = LocatieEncounter.None,
        LocatieId = null,
        Specificatie = null,
    };

    public static LocatieEncounterData Create(LocatieEncounter locatie, string specificatie = null, string locatieId = null)
    {
        var isQuestveld = locatie == LocatieEncounter.Questveld;
        var keptLocatieId = isQuestveld && !string.IsNullOrWhiteSpace(locatieId) ? locatieId : null;

        return new LocatieEncounterData
        {
            Locatie = locatie,
            LocatieId = keptLocatieId,

            // Groeperingsveld keeps its free text; Questveld only when it falls back to "Anders…".
            Specificatie = locatie == LocatieEncounter.Groeperingsveld || (isQuestveld && keptLocatieId is null)
                ? specificatie
                : null,
        };
    }

    public override string ToString()
    {
        if (!HasValue)
            return string.Empty;

        if (RequiresSpecificatie && !string.IsNullOrWhiteSpace(Specificatie))
        {
            return $"{Locatie.Naam()} ({Specificatie})";
        }

        return Locatie.Naam();
    }
}
