namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaPropsEnLootAangepast : AuditTrailEvent
{
    required public IReadOnlyList<PropLootItemData> Props { get; init; }

    required public IReadOnlyList<PropLootItemData> Loot { get; init; }

    public override string What
    {
        get
        {
            var validProps = Props?.Where(p => p.HasValue).ToList() ?? [];
            var validLoot = Loot?.Where(l => l.HasValue).ToList() ?? [];

            if (validProps.Count == 0 && validLoot.Count == 0)
            {
                return "<strong>Props &amp; Loot aangepast:</strong> (leeg)";
            }

            return "<strong>Props &amp; Loot aangepast</strong>";
        }
    }

    public override string Details
    {
        get
        {
            var parts = new List<string>();

            var validProps = Props?.Where(p => p.HasValue).ToList() ?? [];
            if (validProps.Count > 0)
            {
                parts.Add("<strong>Props:</strong>");
                parts.AddRange(validProps.Select(p =>
                    !string.IsNullOrWhiteSpace(p.VerantwoordelijkeSlName)
                        ? $"• {p.Omschrijving} ({p.VerantwoordelijkeSlName})"
                        : $"• {p.Omschrijving}"));
            }

            var validLoot = Loot?.Where(l => l.HasValue).ToList() ?? [];
            if (validLoot.Count > 0)
            {
                parts.Add("<strong>Loot:</strong>");
                parts.AddRange(validLoot.Select(l =>
                    !string.IsNullOrWhiteSpace(l.VerantwoordelijkeSlName)
                        ? $"• {l.Omschrijving} ({l.VerantwoordelijkeSlName})"
                        : $"• {l.Omschrijving}"));
            }

            return parts.Count > 0 ? string.Join("<br/>", parts) : null;
        }
    }
}
