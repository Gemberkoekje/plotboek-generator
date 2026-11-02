namespace PlotboekGenerator.core.Events;

public sealed record PlotboekAangemaaktV2 : AuditTrailEvent
{
    required public Year Jaar { get; init; }

    /// <summary>The event ID, corresponding to the <see cref="LegacyEvent"/> enum member name (e.g. "Moots1", "Summoning").</summary>
    required public string EventId { get; init; }

    required public Dag StartDag { get; init; }

    required public Dag EindDag { get; init; }

    public override string What => $"""
        <strong>Aangemaakt</strong>
        <ul class="mb-0 ps-3">
            <li><strong>Jaar:</strong> {Jaar}</li>
            <li><strong>Event:</strong> {EventId}</li>
            <li><strong>Van:</strong> {StartDag} <strong>t/m</strong> {EindDag}</li>
        </ul>
        """;
}
