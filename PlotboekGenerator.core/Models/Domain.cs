namespace PlotboekGenerator.core.Models;

/// <summary>
/// Represents a domain/category for plotboek paginas (e.g., Tiartal, Gildes, etc.)
/// </summary>
public sealed record Domain
{
    /// <summary>
    /// Unique identifier for the domain.
    /// </summary>
    required public Guid Id { get; init; }

    /// <summary>
    /// Short code for the domain (e.g., "Ti", "G", "C").
    /// </summary>
    required public string Shorthand { get; init; }

    /// <summary>
    /// Full description of the domain (e.g., "Tiartal", "Gildes", "Chaos").
    /// </summary>
    required public string Description { get; init; }

    /// <summary>
    /// Order for display in dropdowns.
    /// </summary>
    required public int Order { get; init; }

    /// <summary>
    /// Creates a new Domain instance.
    /// </summary>
    public static Domain Create(string shorthand, string description, int order)
    {
        return new Domain
        {
            Id = Guid.NewGuid(),
            Shorthand = shorthand,
            Description = description,
            Order = order,
        };
    }

    /// <summary>
    /// Returns display string combining shorthand and description.
    /// </summary>
    public string DisplayName => $"{Shorthand} - {Description}";
}
