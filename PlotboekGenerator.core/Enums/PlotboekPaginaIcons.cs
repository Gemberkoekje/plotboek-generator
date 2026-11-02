namespace PlotboekGenerator.core.Enums;

[Flags]
public enum PlotboekPaginaIcons
{
    None = 0,

    /// <summary>Timing Cruciaal (plot moet in dit blok).</summary>
    TimingCruciaal = 1 << 0,

    /// <summary>Plot Cruciaal. Mag niet gecancelled worden bij te weinig NPCs (gebruiken in overleg met HSL/EC).</summary>
    PlotCruciaal = 1 << 1,

    /// <summary>Mag als eerste gecancelled worden bij te weinig NPCs.</summary>
    MagAlsEersteCancelled = 1 << 2,

    /// <summary>Uitgebreide briefing.</summary>
    UitgebreideBriefing = 1 << 3,

    /// <summary>Groot pak of uitgebreid kostuum/schmink.</summary>
    GrootPakOfKostuum = 1 << 5,

    /// <summary>Muziek vanuit de techniek (see <see cref="Models.Terminologie.MuziekVanuitTechniek"/>).</summary>
    MuziekVanuitTechniek = 1 << 6,

    /// <summary>Specifiek licht/rook vanuit de techniek.</summary>
    LichtOfRookVanuitTechniek = 1 << 7,

    /// <summary>Eventbreed: alle NPCs nodig (see <see cref="Models.Terminologie.EventBreed"/>).</summary>
    EventBreed = 1 << 8,
}
