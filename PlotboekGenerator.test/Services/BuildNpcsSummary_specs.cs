namespace PlotboekGenerator.test.Services;

public sealed class BuildNpcsSummary_specs
{
    [Test]
    public void Returns_space_when_no_value()
    {
        PlotboekExportBuilder.BuildNpcsSummary(AantalNpcsData.Empty).Should().Be(" ");
    }

    [Test]
    public void Returns_ALLE_when_AlleNpcs_is_true()
    {
        var data = new AantalNpcsData { AlleNpcs = true, Rollen = [] };
        PlotboekExportBuilder.BuildNpcsSummary(data).Should().Be("ALLE");
    }

    [Test]
    public void Returns_single_number_when_min_equals_max()
    {
        var data = new AantalNpcsData
        {
            AlleNpcs = false,
            Rollen = [NpcRolData.Create("guard", 3, 3)],
        };
        PlotboekExportBuilder.BuildNpcsSummary(data).Should().Be("3");
    }

    [Test]
    public void Returns_range_when_min_differs_from_max()
    {
        var data = new AantalNpcsData
        {
            AlleNpcs = false,
            Rollen = [NpcRolData.Create("guard", 2, 5)],
        };
        PlotboekExportBuilder.BuildNpcsSummary(data).Should().Be("2 - 5");
    }

    [Test]
    public void Sums_totals_across_multiple_roles()
    {
        var data = new AantalNpcsData
        {
            AlleNpcs = false,
            Rollen =
            [
                NpcRolData.Create("guard", 1, 2),
                NpcRolData.Create("spy", 3, 4),
            ],
        };
        PlotboekExportBuilder.BuildNpcsSummary(data).Should().Be("4 - 6");
    }
}

