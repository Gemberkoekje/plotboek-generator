namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaIsSpelerGedrevenAangepast : AuditTrailEvent
{
    required public bool IsSpelerGedreven { get; init; }

    public override string What => IsSpelerGedreven
        ? "<strong>Encounter gemarkeerd als speler gedreven</strong>"
        : "<strong>Encounter gemarkeerd als niet speler gedreven</strong>";
}
