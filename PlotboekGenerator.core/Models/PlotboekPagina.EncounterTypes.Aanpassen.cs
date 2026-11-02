namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    private Result<PlotboekPagina> AanpassenEncounterTypes(EncounterTypes encounterTypes, string who)
    {
        var major = encounterTypes.Major?.Select(e => e.ToString()).ToList() ?? [];
        var minor = encounterTypes.Minor?.Select(e => e.ToString()).ToList() ?? [];
        return AanpassenEncounterTypes(major, minor, who);
    }

    private Result<PlotboekPagina> AanpassenEncounterTypes(IReadOnlyList<string> major, IReadOnlyList<string> minor, string who)
    {
        var newEncounterTypes = EncounterTypeIds.Create(major, minor);
        if (EncounterTypeIds == newEncounterTypes)
        {
            return Result.For(this);
        }

        return ApplyEvent(new PlotboekPaginaEncounterTypesAangepastV2
        {
            Major = major?.Where(s => s != "None").ToList() ?? [],
            Minor = minor?.Where(s => s != "None").ToList() ?? [],
            Who = who,
        });
    }

    internal void When(PlotboekPaginaEncounterTypesAangepast @event)
    {
        EncounterTypeIds = EncounterTypeIds.Create(
            @event.Major?.Select(e => e.ToString()).ToList() ?? [],
            @event.Minor?.Select(e => e.ToString()).ToList() ?? []);
    }

    internal void When(PlotboekPaginaEncounterTypesAangepastV2 @event)
    {
        EncounterTypeIds = EncounterTypeIds.Create(@event.Major, @event.Minor);
    }
}
