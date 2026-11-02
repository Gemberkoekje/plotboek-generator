namespace PlotboekGenerator.core.Models;

/// <summary>
/// Represents a character name seed for plotboek exports.
/// </summary>
public sealed record CharacterName
{
    /// <summary>
    /// Unique identifier for the character name.
    /// </summary>
    required public Guid Id { get; init; }

    /// <summary>
    /// The character name.
    /// </summary>
    required public string Naam { get; init; }

    /// <summary>
    /// Creates a new CharacterName instance.
    /// </summary>
    public static CharacterName Create(string naam)
    {
        return new CharacterName
        {
            Id = Guid.NewGuid(),
            Naam = naam,
        };
    }
}
