using System.Globalization;
using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Models;

/// <summary>
/// String-based equivalent of <see cref="EncounterTypes"/>, storing encounter type IDs
/// instead of enum values. IDs correspond to <see cref="SoortEncounter"/> member names
/// (e.g. "Vechten", "Roleplay") but are decoupled from the enum for multi-LARP support.
/// </summary>
public readonly record struct EncounterTypeIds : IEquatable<EncounterTypeIds>
{
    public IReadOnlyList<string> Major { get; init; }

    public IReadOnlyList<string> Minor { get; init; }

    public bool HasValue => Major?.Count > 0;

    public static EncounterTypeIds Empty => new()
    {
        Major = [],
        Minor = [],
    };

    public static EncounterTypeIds Create(IEnumerable<string> major, IEnumerable<string> minor = null)
    {
        return new EncounterTypeIds
        {
            Major = major?.Where(s => !string.IsNullOrEmpty(s) && s != "None").ToList() ?? [],
            Minor = minor?.Where(s => !string.IsNullOrEmpty(s) && s != "None").ToList() ?? [],
        };
    }

    public bool Equals(EncounterTypeIds other)
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

    public override string ToString()
    {
        if (!HasValue)
        {
            return string.Empty;
        }

        var display = string.Join(" / ", Major);
        if (Minor?.Count > 0)
        {
            display = string.Format(CultureInfo.CurrentCulture, CoreTekst.EncounterTypesMetMogelijk, display, string.Join(", ", Minor));
        }
        return display;
    }

    private static bool SequenceEqual(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        if (left.Count != right.Count) return false;

        for (var i = 0; i < left.Count; i++)
        {
            if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                return false;
        }
        return true;
    }
}
