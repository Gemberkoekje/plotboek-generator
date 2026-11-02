using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaVolledigKlaarAangepast : AuditTrailEvent
{
    required public bool IsVolledigKlaar { get; init; }

    public override string What => IsVolledigKlaar
        ? $"<strong>{CoreTekst.PlotVolledigKlaar}</strong>"
        : $"<strong>{CoreTekst.PlotNietKlaar}</strong>";
}
