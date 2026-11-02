namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    public LocatieEncounterData LocatieEncounter { get; private set; }

    private Result<PlotboekPagina> AanpassenLocatieEncounter(LocatieEncounterData locatieEncounter, string who)
    {
        if (LocatieEncounter == locatieEncounter)
        {
            return Result.For(this);
        }

        return ApplyEvent(new PlotboekPaginaLocatieEncounterAangepastV2
        {
            Locatie = locatieEncounter.Locatie,
            LocatieId = locatieEncounter.LocatieId,
            Specificatie = locatieEncounter.Specificatie,
            Who = who,
        });
    }

    internal void When(PlotboekPaginaLocatieEncounterAangepast @event)
    {
        // Pre-questlocatie history: there was no curated choice yet, so LocatieId stays empty.
        LocatieEncounter = LocatieEncounterData.Create(@event.Locatie, @event.Specificatie);
    }

    internal void When(PlotboekPaginaLocatieEncounterAangepastV2 @event)
    {
        LocatieEncounter = LocatieEncounterData.Create(@event.Locatie, @event.Specificatie, @event.LocatieId);
    }
}
