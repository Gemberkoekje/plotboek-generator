using System.Collections.Immutable;

namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    public Result<PlotboekPagina> Aanpassen(Data data, PlotboekUser updatedBy)
    {
        ArgumentNullException.ThrowIfNull(data);

        var starttijdId = data.StarttijdId ?? "None";
        var aantalBlokkenId = data.AantalBlokkenId ?? "None";
        var encounterTypeIds = data.EncounterTypeIds.HasValue ? data.EncounterTypeIds : EncounterTypeIds.Empty;

        return Result.For(this)
            | (p => p.AanpassenTitel(data.Titel, updatedBy.Id))
            | (p => p.AanpassenDomain(data.DomainId, data.DomainShorthand, data.DomainDescription, updatedBy.Id))
            | (p => p.AanpassenDag(data.Dag, updatedBy.Id))
            | (p => p.AanpassenStarttijd(starttijdId, updatedBy.Id))
            | (p => p.AanpassenVerantwoordelijkeSl(data.VerantwoordelijkeSlId, data.VerantwoordelijkeSlName, data.ExtraSlIds, data.ExtraSlNames, updatedBy.Id))
            | (p => p.AanpassenAantalBlokken(aantalBlokkenId, updatedBy.Id))
            | (p => p.AanpassenEncounterTypes(encounterTypeIds.Major, encounterTypeIds.Minor, updatedBy.Id))
            | (p => p.AanpassenLocatieEncounter(data.LocatieEncounter, updatedBy.Id))
            | (p => p.AanpassenAantalNpcs(data.AantalNpcs, updatedBy.Id))
            | (p => p.AanpassenElevatorPitch(data.ElevatorPitch, updatedBy.Id))
            | (p => p.AanpassenSamenvattingPlot(data.SamenvattingPlot, updatedBy.Id))
            | (p => p.AanpassenMeerInformatie(data.MeerInformatie, updatedBy.Id))
            | (p => p.AanpassenBijlagen(data.BijlageIds, updatedBy.Id))
            | (p => p.AanpassenPlotlijn(data.PlotlijnId, updatedBy.Id))
            | (p => p.AanpassenVolledigKlaar(data.IsVolledigKlaar, updatedBy.Id))
            | (p => p.AanpassenIsGedraaid(data.IsGedraaid, updatedBy.Id))
            | (p => p.AanpassenIsSpelerGedreven(data.IsSpelerGedreven, updatedBy.Id))
            | (p => p.AanpassenIsFlexibel(data.IsFlexibel, updatedBy.Id))
            | (p => p.AanpassenIsFlexibelNachtPlot(data.IsFlexibelNachtPlot, updatedBy.Id))
            | (p => p.AanpassenInstructiePentagon(data.InstructiePentagon, updatedBy.Id))
            | (p => p.AanpassenIconen(data.Iconen, updatedBy.Id))
            | (p => p.AanpassenPropsEnLoot(data.PropsEnLoot, updatedBy.Id))
            | (p => p.AanpassenOpmerkingen(data.Opmerkingen, updatedBy.Id))
            | (p => p.AanpassenDebrief(data.Debrief, updatedBy.Id));
    }

    public sealed record Data
    {
        required public string Titel { get; init; }

        public Guid DomainId { get; init; }

        public string DomainShorthand { get; init; }

        public string DomainDescription { get; init; }

        required public Dag Dag { get; init; }

        required public string StarttijdId { get; init; }

        required public int Order { get; init; }

        public string VerantwoordelijkeSlId { get; init; }

        public string VerantwoordelijkeSlName { get; init; }

        public ImmutableList<string> ExtraSlIds { get; init; } = [];

        public ImmutableList<string> ExtraSlNames { get; init; } = [];

        public string AantalBlokkenId { get; init; }

        public EncounterTypeIds EncounterTypeIds { get; init; }

        public LocatieEncounterData LocatieEncounter { get; init; }

        public AantalNpcsData AantalNpcs { get; init; }

        public string ElevatorPitch { get; init; }

        public string SamenvattingPlot { get; init; }

        public string MeerInformatie { get; init; }

        /// <summary>
        /// The IDs of the plotboek-level bijlagen this encounter references.
        /// Must be carried on every full save or the aggregate's references are wiped.
        /// </summary>
        public ImmutableList<BijlageId> BijlageIds { get; init; } = [];

        /// <summary>
        /// The ID of the plotlijn this pagina belongs to,
        /// or null if this plot is self-contained.
        /// </summary>
        public PlotlijnId PlotlijnId { get; init; }

        public bool IsVolledigKlaar { get; init; }

        public bool IsGedraaid { get; init; }

        public bool IsSpelerGedreven { get; init; }

        public bool IsFlexibel { get; init; }

        public bool IsFlexibelNachtPlot { get; init; }

        public string InstructiePentagon { get; init; }

        public PlotboekPaginaIcons Iconen { get; init; }

        public PropsEnLootData PropsEnLoot { get; init; }

        public string Opmerkingen { get; init; }

        public DebriefData Debrief { get; init; }
    }
}
