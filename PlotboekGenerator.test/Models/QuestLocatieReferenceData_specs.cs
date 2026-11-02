namespace PlotboekGenerator.test.Models;

public sealed class QuestLocatieReferenceData_specs
{
    private static QuestLocatieCategorieConfig Categorie(string id, int order, QuestLocatieErnst ernst = QuestLocatieErnst.Geen)
        => new () { Id = id, DisplayName = $"Categorie {id}", Ernst = ernst, Order = order };

    private static QuestLocatieConfig Locatie(
        string id,
        string categorieId,
        int order = 1,
        bool isActief = true,
        params string[] conflicteertMet)
        => new ()
        {
            Id = id,
            DisplayName = id,
            CategorieId = categorieId,
            ConflicteertMet = conflicteertMet,
            IsActief = isActief,
            Order = order,
        };

    [Test]
    public void Overlap_is_made_symmetric_so_the_admin_only_fills_in_one_side()
    {
        QuestLocatieConfig[] locaties =
        [
            Locatie("Leeuwenkuil", "Plekken"),
            Locatie("RouteLeeuwenkuil", "Routes", conflicteertMet: "Leeuwenkuil"),
        ];

        var overlap = locaties.BuildSymmetrischeOverlap();

        overlap["RouteLeeuwenkuil"].Should().BeEquivalentTo(["Leeuwenkuil"], "the admin filled this side in");
        overlap["Leeuwenkuil"].Should().BeEquivalentTo(["RouteLeeuwenkuil"], "the reverse is implied");
    }

    [Test]
    public void Overlap_ignores_self_references_and_unknown_ids()
    {
        QuestLocatieConfig[] locaties =
        [
            Locatie("Ruine", "Plekken", conflicteertMet: ["Ruine", "BestaatNiet"]),
        ];

        var overlap = locaties.BuildSymmetrischeOverlap();

        overlap.Should().BeEmpty("a location cannot overlap with itself and unknown ids are dropped");
    }

    [Test]
    public void Grouping_orders_categories_and_locations_and_drops_empty_categories()
    {
        QuestLocatieCategorieConfig[] categorieen = [Categorie("Plekken", 2), Categorie("Routes", 1), Categorie("Leeg", 3)];
        QuestLocatieConfig[] locaties =
        [
            Locatie("PlekB", "Plekken", order: 2),
            Locatie("PlekA", "Plekken", order: 1),
            Locatie("Route", "Routes"),
        ];

        var groups = locaties.GroupByCategorie(categorieen);

        groups.Select(g => g.Categorie.Id).Should().BeEquivalentTo(["Routes", "Plekken"], o => o.WithStrictOrdering());
        groups[1].Locaties.Select(l => l.Id).Should().BeEquivalentTo(["PlekA", "PlekB"], o => o.WithStrictOrdering());
    }

    [Test]
    public void Grouping_hides_inactive_locations_but_keeps_the_current_choice_visible()
    {
        QuestLocatieCategorieConfig[] categorieen = [Categorie("Plekken", 1)];
        QuestLocatieConfig[] locaties =
        [
            Locatie("Actief", "Plekken", order: 1),
            Locatie("Gestopt", "Plekken", order: 2, isActief: false),
            Locatie("OokGestopt", "Plekken", order: 3, isActief: false),
        ];

        var groups = locaties.GroupByCategorie(categorieen, includeInactiveId: "Gestopt");

        groups.Single().Locaties.Select(l => l.Id).Should().BeEquivalentTo(
            ["Actief", "Gestopt"],
            "an old plot must not silently change location, but other retired locations stay hidden");
    }

    [Test]
    public void Locations_pointing_at_an_unknown_category_are_not_offered()
    {
        QuestLocatieCategorieConfig[] categorieen = [Categorie("Plekken", 1)];
        QuestLocatieConfig[] locaties = [Locatie("Zwever", "VerwijderdeCategorie")];

        var groups = locaties.GroupByCategorie(categorieen);

        groups.Should().BeEmpty();
    }

    [Test]
    public void Display_name_falls_back_to_the_raw_id_when_the_config_is_gone()
    {
        QuestLocatieConfig[] locaties = [Locatie("Ruine", "Plekken")];

        locaties.GetDisplayName("Ruine").Should().Be("Ruine");
        locaties.GetDisplayName("Onbekend").Should().Be("Onbekend");
    }
}
