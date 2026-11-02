namespace PlotboekGenerator.core.Events;

public sealed record PlotboekAangemaakt : AuditTrailEvent
{
    required public Year Jaar { get; init; }

    required public LegacyEvent Event { get; init; }

    required public Dag StartDag { get; init; }

    required public Dag EindDag { get; init; }

    public override string What => $"""
        <strong>Aangemaakt</strong>
        <ul class="mb-0 ps-3">
            <li><strong>Jaar:</strong> {Jaar}</li>
            <li><strong>Event:</strong> {Event}</li>
            <li><strong>Van:</strong> {StartDag} <strong>t/m</strong> {EindDag}</li>
        </ul>
        """;
}
