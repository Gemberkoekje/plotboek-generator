using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaDebriefAangepast : AuditTrailEvent
{
    required public bool IsDebriefGewenst { get; init; }

    required public IReadOnlyList<string> Vragen { get; init; }

    public override string What
    {
        get
        {
            if (!IsDebriefGewenst)
            {
                return $"<strong>{CoreTekst.DebriefUitgeschakeld}</strong>";
            }

            var validVragen = Vragen?.Where(v => !string.IsNullOrWhiteSpace(v)).ToList() ?? [];
            if (validVragen.Count == 0)
            {
                return $"<strong>{CoreTekst.DebriefIngeschakeld}</strong>";
            }

            return $"<strong>{CoreTekst.DebriefIngeschakeldMetVragen}</strong>";
        }
    }

    public override string Details
    {
        get
        {
            if (!IsDebriefGewenst)
            {
                return null;
            }

            var validVragen = Vragen?.Where(v => !string.IsNullOrWhiteSpace(v)).ToList() ?? [];
            if (validVragen.Count == 0)
            {
                return null;
            }

            return string.Join("<br/>", validVragen.Select(v => $"• {v}"));
        }
    }
}
