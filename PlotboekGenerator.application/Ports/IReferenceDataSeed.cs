using PlotboekGenerator.core.Models.ReferenceData;

namespace PlotboekGenerator.Application.Ports;

/// <summary>
/// The reference data a deployment starts with: its time slots, block lengths, encounter types,
/// events and quest locations. It is written once, on the first start; after that admins own it.
/// </summary>
/// <remarks>
/// The ids are stable keys that stored events and aggregates refer to: never change the id of a
/// row a deployment has used.
/// </remarks>
public interface IReferenceDataSeed
{
    /// <summary>The start-time slots of an event day.</summary>
    IReadOnlyList<StarttijdConfig> Starttijden { get; }

    /// <summary>The lengths an encounter can take, in blocks.</summary>
    IReadOnlyList<AantalBlokkenConfig> AantalBlokken { get; }

    /// <summary>The kinds of encounter.</summary>
    IReadOnlyList<SoortEncounterConfig> SoortEncounters { get; }

    /// <summary>The events a plotboek can be written for, with their days.</summary>
    IReadOnlyList<EventConfig> Events { get; }

    /// <summary>The groups the quest locations are listed under.</summary>
    IReadOnlyList<QuestLocatieCategorieConfig> QuestLocatieCategorieen { get; }

    /// <summary>The quest locations.</summary>
    IReadOnlyList<QuestLocatieConfig> QuestLocaties { get; }

    /// <summary>
    /// The domains (the groups SLs and encounters belong to), written by the admin page the first
    /// time it finds none.
    /// </summary>
    IReadOnlyList<(string Shorthand, string Description)> Domeinen { get; }

    /// <summary>The fixed moments of an event day, written by the admin page the first time it finds none.</summary>
    IReadOnlyList<(MeetingDagType DagType, string Tijd, string Naam)> Meetings { get; }

    /// <summary>Names to give a character, written by the admin page the first time it finds none.</summary>
    IReadOnlyList<string> KarakterNamen { get; }

    /// <summary>Names of places in the game world, written by the admin page the first time it finds none.</summary>
    IReadOnlyList<string> WereldNamen { get; }
}
