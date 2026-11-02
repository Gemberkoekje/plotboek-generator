namespace PlotboekGenerator.core.Models;

/// <summary>
/// Represents the debrief configuration for an encounter.
/// Contains whether a debrief is required and optional questions for NPCs.
/// </summary>
public readonly record struct DebriefData : IEquatable<DebriefData>
{
    public bool IsDebriefGewenst { get; init; }

    public IReadOnlyList<string> Vragen { get; init; }

    public bool HasValue => IsDebriefGewenst;

    public static DebriefData Empty => new ()
    {
        IsDebriefGewenst = false,
        Vragen = [],
    };

    public static DebriefData Create(bool isDebriefGewenst, IEnumerable<string> vragen)
    {
        return new DebriefData
        {
            IsDebriefGewenst = isDebriefGewenst,
            Vragen = isDebriefGewenst
                ? vragen?.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).ToList() ?? []
                : [],
        };
    }

    /// <summary>
    /// Returns a formatted string for the debrief for DOCX export.
    /// If there are specific questions, each question is followed by 3 empty lines.
    /// If no questions but debrief is requested, returns a header with 3 empty lines for manual writing.
    /// The result must contain non-whitespace text so the DOCX template engine
    /// does not strip the row (it removes rows whose value is whitespace-only).
    /// </summary>
    public string ToDebriefString()
    {
        if (!IsDebriefGewenst)
        {
            return string.Empty;
        }

        var validVragen = Vragen?.Where(v => !string.IsNullOrWhiteSpace(v)).ToList() ?? [];
        if (validVragen.Count == 0)
        {
            return "\n\n\n";
        }

        return string.Join("\n", validVragen.Select(v => $"{v}:\n\n\n"));
    }

    public bool Equals(DebriefData other)
    {
        return IsDebriefGewenst == other.IsDebriefGewenst && SequenceEqual(Vragen, other.Vragen);
    }

    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(IsDebriefGewenst);
        if (Vragen != null)
            foreach (var item in Vragen)
                hash.Add(item);
        return hash.ToHashCode();
    }

    private static bool SequenceEqual(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        if (left.Count != right.Count) return false;
        for (var i = 0; i < left.Count; i++)
        {
            if (left[i] != right[i]) return false;
        }
        return true;
    }
}
