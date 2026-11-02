namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaDomainAangepast : AuditTrailEvent
{
    /// <summary>
    /// The ID of the selected domain, or null if no domain is selected.
    /// </summary>
    public Guid DomainId { get; init; }

    /// <summary>
    /// The shorthand of the selected domain for display purposes.
    /// </summary>
    public string DomainShorthand { get; init; }

    /// <summary>
    /// The description of the selected domain for display purposes.
    /// </summary>
    public string DomainDescription { get; init; }

    public override string What => DomainId != Guid.Empty
        ? $"<strong>Domein aangepast:</strong> {DomainShorthand} - {DomainDescription}"
        : "<strong>Domein verwijderd</strong>";
}
