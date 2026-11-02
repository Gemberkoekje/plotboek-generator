using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaIsSpelerGedrevenAangepast : AuditTrailEvent
{
    required public bool IsSpelerGedreven { get; init; }

    public override string What => IsSpelerGedreven
        ? $"<strong>{CoreTekst.EncounterSpelerGedreven}</strong>"
        : $"<strong>{CoreTekst.EncounterNietSpelerGedreven}</strong>";
}
