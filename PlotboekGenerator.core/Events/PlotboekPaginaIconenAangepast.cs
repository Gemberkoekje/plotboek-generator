using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaIconenAangepast : AuditTrailEvent
{
    required public PlotboekPaginaIcons Iconen { get; init; }

    public override string What
    {
        get
        {
            if (Iconen == PlotboekPaginaIcons.None)
                return CoreTekst.IconenVerwijderd;

            var icons = new List<string>();
            if (Iconen.HasFlag(PlotboekPaginaIcons.TimingCruciaal)) icons.Add(CoreTekst.IcoonTimingCruciaal);
            if (Iconen.HasFlag(PlotboekPaginaIcons.PlotCruciaal)) icons.Add(CoreTekst.IcoonPlotCruciaal);
            if (Iconen.HasFlag(PlotboekPaginaIcons.MagAlsEersteCancelled)) icons.Add(CoreTekst.IcoonMagAlsEersteCancelled);
            if (Iconen.HasFlag(PlotboekPaginaIcons.UitgebreideBriefing)) icons.Add(CoreTekst.IcoonUitgebreideBriefing);
            if (Iconen.HasFlag(PlotboekPaginaIcons.GrootPakOfKostuum)) icons.Add(CoreTekst.IcoonGrootPakOfKostuum);
            if (Iconen.HasFlag(PlotboekPaginaIcons.MuziekVanuitTechniek)) icons.Add(Terminologie.Huidig.MuziekVanuitTechniek);
            if (Iconen.HasFlag(PlotboekPaginaIcons.LichtOfRookVanuitTechniek)) icons.Add(Terminologie.Huidig.LichtOfRookVanuitTechniek);
            if (Iconen.HasFlag(PlotboekPaginaIcons.EventBreed)) icons.Add(Terminologie.Huidig.EventBreed);
            return $"{CoreTekst.IconenAangepast} <strong>{string.Join(", ", icons)}</strong>";
        }
    }
}
