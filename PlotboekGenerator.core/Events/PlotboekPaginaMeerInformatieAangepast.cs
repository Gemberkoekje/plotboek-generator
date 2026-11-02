using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaMeerInformatieAangepast : AuditTrailEvent
{
    required public string MeerInformatie { get; init; }

    public override string What => $"<strong>{CoreTekst.MeerInformatieAangepast}</strong>";

    public override string Details => string.IsNullOrWhiteSpace(MeerInformatie) ? null : MeerInformatie;
}
