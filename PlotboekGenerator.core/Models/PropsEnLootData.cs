namespace PlotboekGenerator.core.Models;

/// <summary>
/// Represents the complete props and loot configuration for an encounter.
/// </summary>
public readonly record struct PropsEnLootData : IEquatable<PropsEnLootData>
{
    public IReadOnlyList<PropLootItemData> Props { get; init; }

    public IReadOnlyList<PropLootItemData> Loot { get; init; }

    public bool HasValue =>
        (Props?.Any(p => p.HasValue) == true) ||
        (Loot?.Any(l => l.HasValue) == true);

    public static PropsEnLootData Empty => new ()
    {
        Props = [],
        Loot = [],
    };

    public static PropsEnLootData Create(
        IEnumerable<PropLootItemData> props,
        IEnumerable<PropLootItemData> loot)
    {
        return new PropsEnLootData
        {
            Props = props?.Where(p => p.HasValue).ToList() ?? [],
            Loot = loot?.Where(l => l.HasValue).ToList() ?? [],
        };
    }

    /// <summary>
    /// Returns a formatted string for the props for DOCX export.
    /// Format: "Prop 1 (SL name)\nProp 2 (Another SL name)".
    /// </summary>
    public string ToPropsString()
    {
        var validProps = Props?.Where(p => p.HasValue).ToList() ?? [];
        return validProps.Count == 0
            ? string.Empty
            : string.Join("\n", validProps.Select(p => p.ToString()));
    }

    /// <summary>
    /// Returns a formatted string for the loot for DOCX export.
    /// Format: "Loot 1 (SL name)\nLoot 2 (Another SL name)".
    /// </summary>
    public string ToLootString()
    {
        var validLoot = Loot?.Where(l => l.HasValue).ToList() ?? [];
        return validLoot.Count == 0
            ? string.Empty
            : string.Join("\n", validLoot.Select(l => l.ToString()));
    }

    public bool Equals(PropsEnLootData other)
    {
        return SequenceEqual(Props, other.Props) && SequenceEqual(Loot, other.Loot);
    }

    public override int GetHashCode()
    {
        var hash = default(HashCode);
        if (Props != null)
            foreach (var item in Props)
                hash.Add(item);
        if (Loot != null)
            foreach (var item in Loot)
                hash.Add(item);
        return hash.ToHashCode();
    }

    private static bool SequenceEqual(IReadOnlyList<PropLootItemData> left, IReadOnlyList<PropLootItemData> right)
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
