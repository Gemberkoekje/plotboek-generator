using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaIsGedraaidAangepast : AuditTrailEvent
{
    required public bool IsGedraaid { get; init; }

    public override string What => IsGedraaid
        ? $"<strong>{CoreTekst.EncounterGedraaid}</strong>"
        : $"<strong>{CoreTekst.EncounterNietGedraaid}</strong>";
}
