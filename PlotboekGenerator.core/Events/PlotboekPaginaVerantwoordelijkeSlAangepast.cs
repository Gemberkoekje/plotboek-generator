using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaVerantwoordelijkeSlAangepast : AuditTrailEvent
{
    required public string VerantwoordelijkeSlId { get; init; }

    required public string VerantwoordelijkeSlName { get; init; }

    public List<string> ExtraSlIds { get; init; } = [];

    public List<string> ExtraSlNames { get; init; } = [];

    public override string What
    {
        get
        {
            var allNames = new List<string>();
            if (!string.IsNullOrEmpty(VerantwoordelijkeSlName))
                allNames.Add(VerantwoordelijkeSlName);
            allNames.AddRange(ExtraSlNames.Where(n => !string.IsNullOrEmpty(n)));
            var display = allNames.Count > 0 ? string.Join(", ", allNames) : CoreTekst.NietIngevuld;
            return $"<strong>{CoreTekst.VerantwoordelijkeSlAangepast}</strong> {display}";
        }
    }
}
