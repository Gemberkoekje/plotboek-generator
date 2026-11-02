namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaGeschatteTijdsduurAangepast : AuditTrailEvent
{
    required public int? MinimumMinuten { get; init; }

    required public int? MaximumMinuten { get; init; }

    public override string What
    {
        get
        {
            var tijdsduur = GeschatteTijdsduur.Create(MinimumMinuten, MaximumMinuten);
            return $"<strong>Geschatte tijdsduur aangepast:</strong> {(tijdsduur.HasValue ? tijdsduur.ToString() : "(leeg)")}";
        }
    }
}
