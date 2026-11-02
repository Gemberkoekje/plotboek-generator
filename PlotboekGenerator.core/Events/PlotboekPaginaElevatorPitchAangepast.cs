namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaElevatorPitchAangepast : AuditTrailEvent
{
    required public string ElevatorPitch { get; init; }

    public override string What => $"<strong>Elevator pitch aangepast:</strong> {ElevatorPitch}";
}
