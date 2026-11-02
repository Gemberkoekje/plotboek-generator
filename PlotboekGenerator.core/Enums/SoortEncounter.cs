namespace PlotboekGenerator.core.Enums;

/// <summary>
/// Use <see cref="PlotboekGenerator.core.Models.ReferenceData.SoortEncounterConfig"/> loaded from the database instead.
/// This enum is retained only for deserialising historical domain events.
/// </summary>
[Obsolete("Use SoortEncounterConfig loaded from the database. This enum exists only for backward-compatible event deserialisation.")]
public enum SoortEncounter
{
    None = 0,
    Vechten,
    Roleplay,
    Ritueel,
    Handel,
    Politiek,
    Magie,
    Praten,
    Puzzel,
}
