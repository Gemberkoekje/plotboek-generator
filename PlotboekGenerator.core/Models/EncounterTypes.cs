namespace PlotboekGenerator.core.Models;

/// <summary>
/// Represents the encounter types with major and minor classifications.
/// Major types are the primary encounter types, minor types are secondary/possible encounter types.
/// </summary>
/// <remarks>
/// Examples:
/// - Major: [Vechten], Minor: [Roleplay] → "Vechten (mogelijk Roleplay)"
/// - Major: [Roleplay, Handel], Minor: [] → "Roleplay / Handel"
/// - Major: [Vechten], Minor: [Roleplay, Magie] → "Vechten (mogelijk Roleplay, Magie)"
/// </remarks>
public readonly record struct EncounterTypes : IEquatable<EncounterTypes>
{
    public IReadOnlyList<SoortEncounter> Major { get; init; }
    public IReadOnlyList<SoortEncounter> Minor { get; init; }

    public bool HasValue => Major?.Count > 0;

    public static EncounterTypes Empty => new ()
    {
        Major = [],
        Minor = [],
    };

    public static EncounterTypes Create(IEnumerable<SoortEncounter> major, IEnumerable<SoortEncounter> minor = null)
    {
        return new EncounterTypes
        {
            Major = major?.Where(e => e != SoortEncounter.None).ToList() ?? [],
            Minor = minor?.Where(e => e != SoortEncounter.None).ToList() ?? [],
        };
    }

    public bool Equals(EncounterTypes other)
    {
        return SequenceEqual(Major, other.Major) && SequenceEqual(Minor, other.Minor);
    }

    public override int GetHashCode()
    {
        var hash = default(HashCode);
        if (Major != null)
        {
            foreach (var item in Major)
                hash.Add(item);
        }
        if (Minor != null)
        {
            foreach (var item in Minor)
                hash.Add(item);
        }
        return hash.ToHashCode();
    }

    private static bool SequenceEqual(IReadOnlyList<SoortEncounter> left, IReadOnlyList<SoortEncounter> right)
    {
        if (left is null && right is null)
            return true;
        if (left is null || right is null)
            return false;
        if (left.Count != right.Count)
            return false;
        for (var i = 0; i < left.Count; i++)
        {
            if (left[i] != right[i])
                return false;
        }
        return true;
    }

    public override string ToString()
    {
        if (!HasValue)
            return string.Empty;

        var majorText = string.Join(" / ", Major);

        if (Minor?.Count > 0)
        {
            var minorText = string.Join(", ", Minor);
            return $"{majorText} (mogelijk {minorText})";
        }

        return majorText;
    }
}
