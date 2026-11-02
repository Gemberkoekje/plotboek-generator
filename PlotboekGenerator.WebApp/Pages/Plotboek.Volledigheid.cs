namespace PlotboekGenerator.WebApp.Pages;

/// <summary>
/// The required-field rules behind "Plot volledig klaar": which fields a plot still misses, and whether
/// only the final tick is left. Kept in one place so the detail panel and the plot list always agree.
/// </summary>
public sealed partial class PlotboekModel
{
    /// <summary>
    /// The required fields a pagina still misses, grouped by the detail section that shows them.
    /// </summary>
    public sealed record OntbrekendeVelden
    {
        /// <summary>Missing fields of the title section (name and domain).</summary>
        required public IReadOnlyList<string> Titel { get; init; }

        /// <summary>Missing fields of the planning section (SL, dagdeel, duration).</summary>
        required public IReadOnlyList<string> Planning { get; init; }

        /// <summary>Missing fields of the encounter section (type and location).</summary>
        required public IReadOnlyList<string> Encounter { get; init; }

        /// <summary>Missing fields of the NPC section.</summary>
        required public IReadOnlyList<string> Npc { get; init; }

        /// <summary>Missing fields of the plot section (elevator pitch and summary).</summary>
        required public IReadOnlyList<string> Plot { get; init; }

        /// <summary>Missing fields of the props and loot section.</summary>
        required public IReadOnlyList<string> PropsEnLoot { get; init; }

        /// <summary>Every missing field, in the order the detail panel lists its sections.</summary>
        public IReadOnlyList<string> Alle => [.. Titel, .. Planning, .. Encounter, .. Npc, .. Plot, .. PropsEnLoot];

        /// <summary>Whether no required field is missing, so the pagina may be marked "volledig klaar".</summary>
        public bool IsCompleet => Alle.Count == 0;
    }

    /// <summary>
    /// Determines which required fields <paramref name="pagina"/> still misses.
    /// </summary>
    /// <param name="pagina">The pagina to check.</param>
    /// <param name="isEvergreen">Whether the pagina lives in the Evergreen plotboek, which has no dagdeel.</param>
    /// <returns>The missing fields per section; <see cref="OntbrekendeVelden.IsCompleet"/> when none.</returns>
    internal static OntbrekendeVelden BepaalOntbrekendeVelden(PlotboekPaginaInfo pagina, bool isEvergreen)
    {
        ArgumentNullException.ThrowIfNull(pagina);

        var titel = new List<string>();
        if (string.IsNullOrEmpty(pagina.Title)) titel.Add("Naam Encounter");
        if (pagina.DomainId == Guid.Empty) titel.Add("Domein");

        var planning = new List<string>();
        if (string.IsNullOrEmpty(pagina.VerantwoordelijkeSlName)) planning.Add("Verantwoordelijke SL");
        if (!pagina.IsSpelerGedreven && !isEvergreen && pagina.StarttijdId == "None") planning.Add("Dagdeel");
        if (pagina.AantalBlokkenId == "None") planning.Add("Geschatte Tijdsduur");

        var encounter = new List<string>();
        if (!pagina.EncounterTypeIds.HasValue) encounter.Add("Soort Encounter");
        if (!pagina.LocatieEncounter.HasValue) encounter.Add("Locatie Encounter");

        var npc = new List<string>();
        if (!pagina.AantalNpcs.HasValue) npc.Add("Aantal NPCs");
        if (!pagina.AantalNpcs.GeenNpcsNodig && pagina.AantalNpcs.Rollen?.Any(r => r.HasValue && !string.IsNullOrWhiteSpace(r.KledingEnSchmink)) != true) npc.Add("Kleding & Schmink");

        var plot = new List<string>();
        if (string.IsNullOrEmpty(pagina.ElevatorPitch)) plot.Add("Elevator Pitch");
        if (string.IsNullOrEmpty(pagina.SamenvattingPlot)) plot.Add("Samenvatting Plot");

        var propsEnLoot = new List<string>();
        if (pagina.PropsEnLoot.Props?.Any(IsSlZonderNaam) == true) propsEnLoot.Add("Verantwoordelijke SL bij prop(s)");
        if (pagina.PropsEnLoot.Loot?.Any(IsSlZonderNaam) == true) propsEnLoot.Add("Verantwoordelijke SL bij loot");

        return new OntbrekendeVelden
        {
            Titel = titel,
            Planning = planning,
            Encounter = encounter,
            Npc = npc,
            Plot = plot,
            PropsEnLoot = propsEnLoot,
        };
    }

    /// <summary>
    /// Whether the only thing left on <paramref name="pagina"/> is ticking "Plot volledig klaar": every
    /// required field is filled in, it isn't klaar (or gedraaid) yet, and no open feedback blocks the tick.
    /// Drives the reminder on the klaar switch and the bell in the plot list.
    /// </summary>
    /// <param name="pagina">The pagina to check.</param>
    /// <param name="isEvergreen">Whether the pagina lives in the Evergreen plotboek.</param>
    /// <returns><see langword="true"/> when the SL only has to tick the box.</returns>
    internal static bool IsAlleenVinkjeNog(PlotboekPaginaInfo pagina, bool isEvergreen)
    {
        ArgumentNullException.ThrowIfNull(pagina);

        return !pagina.IsVolledigKlaar
            && !pagina.IsGedraaid
            && !pagina.HeeftOpenstaandeFeedback
            && BepaalOntbrekendeVelden(pagina, isEvergreen).IsCompleet;
    }

    // An item an SL has to bring, but without the SL picked yet.
    private static bool IsSlZonderNaam(PropLootItemData item)
        => item.HasValue
            && string.Equals(item.VerantwoordelijkeSource, "SL", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(item.VerantwoordelijkeSlId);
}
