using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Models;

/// <summary>
/// The names people read for the enums they see, in their language. Use these, never
/// <c>ToString()</c>: the stored names are code and stay Dutch.
/// </summary>
public static class EnumTekst
{
    /// <summary>The name of a day.</summary>
    /// <param name="dag">The day.</param>
    /// <returns>Its name, e.g. "Monday".</returns>
    public static string Naam(this Dag dag) => dag switch
    {
        Dag.Maandag => CoreTekst.DagMaandag,
        Dag.Dinsdag => CoreTekst.DagDinsdag,
        Dag.Woensdag => CoreTekst.DagWoensdag,
        Dag.Donderdag => CoreTekst.DagDonderdag,
        Dag.Vrijdag => CoreTekst.DagVrijdag,
        Dag.Zaterdag => CoreTekst.DagZaterdag,
        Dag.Zondag => CoreTekst.DagZondag,
        _ => CoreTekst.DagGeen,
    };

    /// <summary>The name of an encounter's location.</summary>
    /// <param name="locatie">The location.</param>
    /// <returns>Its name, e.g. "Main field".</returns>
    public static string Naam(this LocatieEncounter locatie) => locatie switch
    {
        LocatieEncounter.Hoofdveld => CoreTekst.LocatieEncounterHoofdveld,
        LocatieEncounter.Groeperingsveld => CoreTekst.LocatieEncounterGroeperingsveld,
        LocatieEncounter.Questveld => CoreTekst.LocatieEncounterQuestveld,
        _ => CoreTekst.LocatieEncounterGeen,
    };

    /// <summary>The name of a kind of event day.</summary>
    /// <param name="dagType">The kind of day.</param>
    /// <returns>Its name, e.g. "First day".</returns>
    public static string Naam(this MeetingDagType dagType) => dagType switch
    {
        MeetingDagType.EersteDag => CoreTekst.MeetingDagTypeEersteDag,
        MeetingDagType.LaatseDag => CoreTekst.MeetingDagTypeLaatsteDag,
        _ => CoreTekst.MeetingDagTypeTussendag,
    };

    /// <summary>The name of a feedback point's status.</summary>
    /// <param name="status">The status.</param>
    /// <returns>Its name, e.g. "Answered".</returns>
    public static string Naam(this FeedbackStatus status) => status switch
    {
        FeedbackStatus.Open => CoreTekst.FeedbackStatusOpen,
        FeedbackStatus.Beantwoord => CoreTekst.FeedbackStatusBeantwoord,
        FeedbackStatus.Afgehandeld => CoreTekst.FeedbackStatusAfgehandeld,
        _ => CoreTekst.FeedbackStatusGeen,
    };

    /// <summary>The name of how strongly a quest location is advised against.</summary>
    /// <param name="ernst">The severity.</param>
    /// <returns>Its name, e.g. "Caution".</returns>
    public static string Naam(this QuestLocatieErnst ernst) => ernst switch
    {
        QuestLocatieErnst.LetOp => CoreTekst.QuestLocatieErnstLetOp,
        QuestLocatieErnst.Afgeraden => CoreTekst.QuestLocatieErnstAfgeraden,
        _ => CoreTekst.QuestLocatieErnstGeen,
    };
}
