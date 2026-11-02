namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaIconenAangepast : AuditTrailEvent
{
    required public PlotboekPaginaIcons Iconen { get; init; }

    public override string What
    {
        get
        {
            if (Iconen == PlotboekPaginaIcons.None)
                return "Iconen verwijderd";

            var icons = new List<string>();
            if (Iconen.HasFlag(PlotboekPaginaIcons.TimingCruciaal)) icons.Add("Timing Cruciaal");
            if (Iconen.HasFlag(PlotboekPaginaIcons.PlotCruciaal)) icons.Add("Plot Cruciaal");
            if (Iconen.HasFlag(PlotboekPaginaIcons.MagAlsEersteCancelled)) icons.Add("Mag als eerste gecancelled");
            if (Iconen.HasFlag(PlotboekPaginaIcons.UitgebreideBriefing)) icons.Add("Uitgebreide briefing");
            if (Iconen.HasFlag(PlotboekPaginaIcons.GrootPakOfKostuum)) icons.Add("Groot pak of kostuum/schmink");
            if (Iconen.HasFlag(PlotboekPaginaIcons.MuziekVanuitTechniek)) icons.Add(Terminologie.Huidig.MuziekVanuitTechniek);
            if (Iconen.HasFlag(PlotboekPaginaIcons.LichtOfRookVanuitTechniek)) icons.Add(Terminologie.Huidig.LichtOfRookVanuitTechniek);
            if (Iconen.HasFlag(PlotboekPaginaIcons.EventBreed)) icons.Add(Terminologie.Huidig.EventBreed);
            return $"Iconen aangepast: <strong>{string.Join(", ", icons)}</strong>";
        }
    }
}
