using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaIsVerwijderdAangepast : AuditTrailEvent
{
    required public bool IsVerwijderd { get; init; }

    public override string What => IsVerwijderd
        ? $"<strong>{CoreTekst.PaginaVerwijderd}</strong>"
        : $"<strong>{CoreTekst.PaginaHersteld}</strong>";
}
