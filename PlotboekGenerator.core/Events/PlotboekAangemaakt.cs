using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekAangemaakt : AuditTrailEvent
{
    required public Year Jaar { get; init; }

    required public LegacyEvent Event { get; init; }

    required public Dag StartDag { get; init; }

    required public Dag EindDag { get; init; }

    public override string What => $"""
        <strong>{CoreTekst.HistorieAangemaakt}</strong>
        <ul class="mb-0 ps-3">
            <li><strong>{CoreTekst.HistorieJaar}</strong> {Jaar}</li>
            <li><strong>{CoreTekst.HistorieEvent}</strong> {Event}</li>
            <li><strong>{CoreTekst.HistorieVan}</strong> {StartDag.Naam()} <strong>{CoreTekst.HistorieTotEnMet}</strong> {EindDag.Naam()}</li>
        </ul>
        """;
}
