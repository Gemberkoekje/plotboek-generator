namespace PlotboekGenerator.test.Aggregates;

public sealed class PlotboekPagina_VervangenSlUser_specs
{
    private static PlotboekUser TestUser => new ()
    {
        Id = "admin-1",
        Lidnummer = "12345",
        FirstName = "Admin",
        LastName = "User",
        UserName = "admin",
        Email = "admin@example.test",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsDummy = false,
        PrimaryDomainId = Guid.Empty,
    };

    private static PlotboekPagina CreatePaginaWithSl(
        string verantwoordelijkeSlId,
        List<string>? extraSlIds = null,
        PropsEnLootData? propsEnLoot = null)
    {
        var pagina = new PlotboekPagina();
        var plotboekId = PlotboekId.Create(Year.Create(2025), EventIds.Voorjaar);
        var addResult = pagina.AanPlotboekToevoegen(plotboekId, "Voorjaar 2025", TestUser);
        pagina = addResult.Value;

        var data = new PlotboekPagina.Data
        {
            Titel = "Test Encounter",
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            Order = 1,
            VerantwoordelijkeSlId = verantwoordelijkeSlId,
            VerantwoordelijkeSlName = $"Name-{verantwoordelijkeSlId}",
            ExtraSlIds = [.. extraSlIds ?? []],
            ExtraSlNames = [.. (extraSlIds ?? []).Select(id => $"Name-{id}")],
            PropsEnLoot = propsEnLoot ?? PropsEnLootData.Empty,
        };
        var aanpassenResult = pagina.Aanpassen(data, TestUser);
        pagina = aanpassenResult.Value;

        pagina.MarkAllAsCommitted();
        return pagina;
    }

    [Test]
    public void ReferencesSlUser_true_when_verantwoordelijke()
    {
        var pagina = CreatePaginaWithSl("sl-source");

        pagina.ReferencesSlUser("sl-source").Should().BeTrue();
    }

    [Test]
    public void ReferencesSlUser_true_when_in_extra_sls()
    {
        var pagina = CreatePaginaWithSl("sl-other", ["sl-source", "sl-another"]);

        pagina.ReferencesSlUser("sl-source").Should().BeTrue();
    }

    [Test]
    public void ReferencesSlUser_false_when_not_referenced()
    {
        var pagina = CreatePaginaWithSl("sl-other");

        pagina.ReferencesSlUser("sl-source").Should().BeFalse();
    }

    [Test]
    public void ReferencesSlUserInPropsEnLoot_true_when_in_props()
    {
        var propsEnLoot = PropsEnLootData.Create(
            [PropLootItemData.Create("Zwaard", "sl-source", "Source SL")],
            []);
        var pagina = CreatePaginaWithSl("sl-other", propsEnLoot: propsEnLoot);

        pagina.ReferencesSlUserInPropsEnLoot("sl-source").Should().BeTrue();
    }

    [Test]
    public void ReferencesSlUserInPropsEnLoot_true_when_in_loot()
    {
        var propsEnLoot = PropsEnLootData.Create(
            [],
            [PropLootItemData.Create("Goud", "sl-source", "Source SL")]);
        var pagina = CreatePaginaWithSl("sl-other", propsEnLoot: propsEnLoot);

        pagina.ReferencesSlUserInPropsEnLoot("sl-source").Should().BeTrue();
    }

    [Test]
    public void ReferencesSlUserInPropsEnLoot_false_when_not_referenced()
    {
        var propsEnLoot = PropsEnLootData.Create(
            [PropLootItemData.Create("Zwaard", "sl-other", "Other SL")],
            []);
        var pagina = CreatePaginaWithSl("sl-other", propsEnLoot: propsEnLoot);

        pagina.ReferencesSlUserInPropsEnLoot("sl-source").Should().BeFalse();
    }

    [Test]
    public void VervangenSlUser_replaces_verantwoordelijke_sl()
    {
        var userNameLookup = new Dictionary<string, string>
        {
            ["sl-target"] = "Target Name",
        };
        var pagina = CreatePaginaWithSl("sl-source");

        var result = pagina.VervangenSlUser("sl-source", "sl-target", "Target Name", userNameLookup, "admin-1");

        result.IsValid.Should().BeTrue();
        result.Value.VerantwoordelijkeSl.Should().Be("sl-target");
    }

    [Test]
    public void VervangenSlUser_replaces_in_extra_sls()
    {
        var userNameLookup = new Dictionary<string, string>
        {
            ["sl-other"] = "Other Name",
            ["sl-target"] = "Target Name",
        };
        var pagina = CreatePaginaWithSl("sl-other", ["sl-source", "sl-another"]);

        // Also add sl-another to lookup
        userNameLookup["sl-another"] = "Another Name";

        var result = pagina.VervangenSlUser("sl-source", "sl-target", "Target Name", userNameLookup, "admin-1");

        result.IsValid.Should().BeTrue();
        result.Value.ExtraSlIds.Should().Contain("sl-target");
        result.Value.ExtraSlIds.Should().NotContain("sl-source");
    }

    [Test]
    public void VervangenSlUser_replaces_in_props()
    {
        var propsEnLoot = PropsEnLootData.Create(
            [PropLootItemData.Create("Zwaard", "sl-source", "Source SL")],
            []);
        var pagina = CreatePaginaWithSl("sl-other", propsEnLoot: propsEnLoot);
        var userNameLookup = new Dictionary<string, string>
        {
            ["sl-other"] = "Other Name",
        };

        var result = pagina.VervangenSlUser("sl-source", "sl-target", "Target Name", userNameLookup, "admin-1");

        result.IsValid.Should().BeTrue();
        result.Value.PropsEnLoot.Props[0].VerantwoordelijkeSlId.Should().Be("sl-target");
        result.Value.PropsEnLoot.Props[0].VerantwoordelijkeSlName.Should().Be("Target Name");
    }

    [Test]
    public void VervangenSlUser_replaces_in_loot()
    {
        var propsEnLoot = PropsEnLootData.Create(
            [],
            [PropLootItemData.Create("Goud", "sl-source", "Source SL")]);
        var pagina = CreatePaginaWithSl("sl-other", propsEnLoot: propsEnLoot);
        var userNameLookup = new Dictionary<string, string>
        {
            ["sl-other"] = "Other Name",
        };

        var result = pagina.VervangenSlUser("sl-source", "sl-target", "Target Name", userNameLookup, "admin-1");

        result.IsValid.Should().BeTrue();
        result.Value.PropsEnLoot.Loot[0].VerantwoordelijkeSlId.Should().Be("sl-target");
        result.Value.PropsEnLoot.Loot[0].VerantwoordelijkeSlName.Should().Be("Target Name");
    }

    [Test]
    public void VervangenSlUser_noop_when_not_referenced()
    {
        var pagina = CreatePaginaWithSl("sl-other");
        var userNameLookup = new Dictionary<string, string>
        {
            ["sl-other"] = "Other Name",
        };

        var result = pagina.VervangenSlUser("sl-source", "sl-target", "Target Name", userNameLookup, "admin-1");

        result.IsValid.Should().BeTrue();
        result.Value.VerantwoordelijkeSl.Should().Be("sl-other");
    }

    [Test]
    public void VervangenSlUser_replaces_both_verantwoordelijke_and_props()
    {
        var propsEnLoot = PropsEnLootData.Create(
            [PropLootItemData.Create("Zwaard", "sl-source", "Source SL")],
            [PropLootItemData.Create("Goud", "sl-source", "Source SL")]);
        var pagina = CreatePaginaWithSl("sl-source", propsEnLoot: propsEnLoot);
        var userNameLookup = new Dictionary<string, string>
        {
            ["sl-target"] = "Target Name",
        };

        var result = pagina.VervangenSlUser("sl-source", "sl-target", "Target Name", userNameLookup, "admin-1");

        result.IsValid.Should().BeTrue();
        result.Value.VerantwoordelijkeSl.Should().Be("sl-target");
        result.Value.PropsEnLoot.Props[0].VerantwoordelijkeSlId.Should().Be("sl-target");
        result.Value.PropsEnLoot.Loot[0].VerantwoordelijkeSlId.Should().Be("sl-target");
    }

    [Test]
    public void VervangenSlUser_leaves_other_props_untouched()
    {
        var propsEnLoot = PropsEnLootData.Create(
            [
                PropLootItemData.Create("Zwaard", "sl-source", "Source SL"),
                PropLootItemData.Create("Schild", "sl-other", "Other SL"),
            ],
            []);
        var pagina = CreatePaginaWithSl("sl-other", propsEnLoot: propsEnLoot);
        var userNameLookup = new Dictionary<string, string>
        {
            ["sl-other"] = "Other Name",
        };

        var result = pagina.VervangenSlUser("sl-source", "sl-target", "Target Name", userNameLookup, "admin-1");

        result.IsValid.Should().BeTrue();
        result.Value.PropsEnLoot.Props[0].VerantwoordelijkeSlId.Should().Be("sl-target");
        result.Value.PropsEnLoot.Props[1].VerantwoordelijkeSlId.Should().Be("sl-other");
    }
}
