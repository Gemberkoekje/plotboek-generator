namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaIsGedraaidAangepast : AuditTrailEvent
{
    required public bool IsGedraaid { get; init; }

    public override string What => IsGedraaid
        ? "<strong>Encounter gemarkeerd als gedraaid</strong>"
        : "<strong>Encounter gemarkeerd als niet gedraaid</strong>";
}
