namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaIsVerwijderdAangepast : AuditTrailEvent
{
    required public bool IsVerwijderd { get; init; }

    public override string What => IsVerwijderd
        ? "<strong>Pagina verwijderd</strong>"
        : "<strong>Pagina hersteld</strong>";
}
