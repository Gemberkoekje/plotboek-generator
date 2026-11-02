namespace PlotboekGenerator.core.Events;

/// <summary>
/// Replaces <see cref="PlotboekPaginaLocatieEncounterAangepast"/> by adding the curated questlocatie
/// choice. The v1 event stays in place for replay of existing streams; new writes always use this one.
/// </summary>
public sealed record PlotboekPaginaLocatieEncounterAangepastV2 : AuditTrailEvent
{
    required public LocatieEncounter Locatie { get; init; }

    /// <summary>The chosen <see cref="Models.ReferenceData.QuestLocatieConfig"/> id, empty for free text.</summary>
    public string LocatieId { get; init; }

    public string Specificatie { get; init; }

    public override string What
    {
        get
        {
            var locatieData = LocatieEncounterData.Create(Locatie, Specificatie, LocatieId);
            if (!locatieData.HasValue)
            {
                return "<strong>Locatie encounter aangepast:</strong> (leeg)";
            }

            // Both parts carry free text the user typed, and the audit modal writes What with
            // Html.Raw — so they have to arrive encoded.
            // The raw id is shown here on purpose: resolving it would freeze the display name of the
            // moment, while a rename should flow through to the whole history.
            return locatieData.HasQuestLocatieKeuze
                ? $"<strong>Locatie encounter aangepast:</strong> {PlainTextInline(locatieData.ToString())} ({PlainTextInline(locatieData.LocatieId)})"
                : $"<strong>Locatie encounter aangepast:</strong> {PlainTextInline(locatieData.ToString())}";
        }
    }
}
