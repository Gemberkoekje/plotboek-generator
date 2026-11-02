namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    public string ElevatorPitch { get; private set; }

    private Result<PlotboekPagina> AanpassenElevatorPitch(string elevatorPitch, string who)
    {
        if (string.Equals(ElevatorPitch ?? string.Empty, elevatorPitch ?? string.Empty, StringComparison.Ordinal))
        {
            return Result.For(this);
        }

        return ApplyEvent(new PlotboekPaginaElevatorPitchAangepast
        {
            ElevatorPitch = elevatorPitch,
            Who = who,
        });
    }

    internal void When(PlotboekPaginaElevatorPitchAangepast @event)
    {
        ElevatorPitch = @event.ElevatorPitch;
    }
}
