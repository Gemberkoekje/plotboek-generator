namespace PlotboekGenerator.core.Models;

/// <summary>
/// Represents a world name seed for plotboek exports.
/// </summary>
public sealed record WorldName
{
    /// <summary>
    /// Unique identifier for the world name.
    /// </summary>
    required public Guid Id { get; init; }

    /// <summary>
    /// The world name.
    /// </summary>
    required public string Naam { get; init; }

    /// <summary>
    /// Creates a new WorldName instance.
    /// </summary>
    public static WorldName Create(string naam)
    {
        return new WorldName
        {
            Id = Guid.NewGuid(),
            Naam = naam,
        };
    }
}
