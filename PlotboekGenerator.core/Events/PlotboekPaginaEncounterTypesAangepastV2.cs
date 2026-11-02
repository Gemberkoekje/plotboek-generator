namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaEncounterTypesAangepastV2 : AuditTrailEvent
{
    /// <summary>The major encounter type IDs, corresponding to <see cref="SoortEncounter"/> enum member names (e.g. "Vechten", "Roleplay").</summary>
    required public IReadOnlyList<string> Major { get; init; }

    /// <summary>The minor encounter type IDs, corresponding to <see cref="SoortEncounter"/> enum member names (e.g. "Vechten", "Roleplay").</summary>
    required public IReadOnlyList<string> Minor { get; init; }

    public override string What
    {
        get
        {
            var major = Major?.Where(s => s != "None").ToList() ?? [];
            var minor = Minor?.Where(s => s != "None").ToList() ?? [];

            if (major.Count == 0)
            {
                return "<strong>Soort encounter aangepast:</strong> (leeg)";
            }

            var display = string.Join(" / ", major);
            if (minor.Count > 0)
            {
                display += $" (mogelijk {string.Join(", ", minor)})";
            }

            return $"<strong>Soort encounter aangepast:</strong> {display}";
        }
    }
}
