namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaIsFlexibelAangepast : AuditTrailEvent
{
    required public bool IsFlexibel { get; init; }

    public override string What => IsFlexibel
        ? "<strong>Encounter gemarkeerd als flexibel</strong>"
        : "<strong>Encounter gemarkeerd als niet flexibel</strong>";
}
