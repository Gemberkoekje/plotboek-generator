using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekVolledigKlaarAangepast : AuditTrailEvent
{
    required public bool IsVolledigKlaar { get; init; }

    public override string What => IsVolledigKlaar
        ? $"<strong>{CoreTekst.PlotboekVolledigKlaar}</strong>"
        : $"<strong>{CoreTekst.PlotboekNietKlaar}</strong>";
}
