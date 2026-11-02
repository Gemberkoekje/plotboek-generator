namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaInstructiePentagonAangepast : AuditTrailEvent
{
    required public string InstructiePentagon { get; init; }

    public override string What => $"<strong>{Terminologie.Huidig.InstructieTechniek} aangepast</strong>";

    public override string Details => string.IsNullOrWhiteSpace(InstructiePentagon) ? null : InstructiePentagon;
}
