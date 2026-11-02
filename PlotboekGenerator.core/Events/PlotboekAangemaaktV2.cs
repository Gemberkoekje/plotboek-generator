using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekAangemaaktV2 : AuditTrailEvent
{
    required public Year Jaar { get; init; }

    /// <summary>The event ID, corresponding to the <see cref="LegacyEvent"/> enum member name (e.g. "Moots1", "Summoning").</summary>
    required public string EventId { get; init; }

    required public Dag StartDag { get; init; }

    required public Dag EindDag { get; init; }

    public override string What => $"""
        <strong>{CoreTekst.HistorieAangemaakt}</strong>
        <ul class="mb-0 ps-3">
            <li><strong>{CoreTekst.HistorieJaar}</strong> {Jaar}</li>
            <li><strong>{CoreTekst.HistorieEvent}</strong> {EventId}</li>
            <li><strong>{CoreTekst.HistorieVan}</strong> {StartDag.Naam()} <strong>{CoreTekst.HistorieTotEnMet}</strong> {EindDag.Naam()}</li>
        </ul>
        """;
}
