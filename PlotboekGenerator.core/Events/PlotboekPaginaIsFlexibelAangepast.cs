using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaIsFlexibelAangepast : AuditTrailEvent
{
    required public bool IsFlexibel { get; init; }

    public override string What => IsFlexibel
        ? $"<strong>{CoreTekst.EncounterFlexibel}</strong>"
        : $"<strong>{CoreTekst.EncounterNietFlexibel}</strong>";
}
