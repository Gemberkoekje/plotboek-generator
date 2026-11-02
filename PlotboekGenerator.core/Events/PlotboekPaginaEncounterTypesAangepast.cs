namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaEncounterTypesAangepast : AuditTrailEvent
{
    required public IReadOnlyList<SoortEncounter> Major { get; init; }

    required public IReadOnlyList<SoortEncounter> Minor { get; init; }

    public override string What
    {
        get
        {
            var encounterTypes = EncounterTypes.Create(Major, Minor);
            return $"<strong>Soort encounter aangepast:</strong> {(encounterTypes.HasValue ? encounterTypes.ToString() : "(leeg)")}";
        }
    }
}
