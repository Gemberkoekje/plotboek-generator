namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaLocatieEncounterAangepast : AuditTrailEvent
{
    required public LocatieEncounter Locatie { get; init; }

    public string Specificatie { get; init; }

    public override string What
    {
        get
        {
            // Specificatie is free text and the audit modal writes What with Html.Raw, so it has to
            // arrive encoded — including for streams written before the V2 event existed.
            var locatieData = LocatieEncounterData.Create(Locatie, Specificatie);
            return $"<strong>Locatie encounter aangepast:</strong> {(locatieData.HasValue ? PlainTextInline(locatieData.ToString()) : "(leeg)")}";
        }
    }
}
