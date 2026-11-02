namespace PlotboekGenerator.test.Pages;

public sealed class PlotboekModel_LoadSelectedPagina_specs
{
    private static PlotboekModel CreateModel()
    {
        var model = new PlotboekModel(null!, null!, null!, null!, null!, null!, null!, null!, null!);
        model.AvailableStarttijdConfigs =
        [
            new StarttijdConfig { Id = StarttijdIds.None, EnumValue = 0, DisplayName = "-", IsNone = true, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = false, IsAvailableOnLaatsteDag = false, Order = 0 },
            new StarttijdConfig { Id = StarttijdIds.Tijd1030, EnumValue = 1, DisplayName = "10:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = true, Order = 1 },
            new StarttijdConfig { Id = StarttijdIds.Tijd1230, EnumValue = 2, DisplayName = "12:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = true, Order = 2 },
            new StarttijdConfig { Id = StarttijdIds.Tijd1430, EnumValue = 3, DisplayName = "14:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = true, Order = 3 },
            new StarttijdConfig { Id = StarttijdIds.Tijd1630, EnumValue = 4, DisplayName = "16:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = false, Order = 4 },
            new StarttijdConfig { Id = StarttijdIds.Tijd1930Introquest, EnumValue = 5, DisplayName = "19:30: Introquest", IsNone = false, IsAvailableOnEersteDag = true, IsAvailableOnMiddelDagen = false, IsAvailableOnLaatsteDag = false, Order = 5 },
            new StarttijdConfig { Id = StarttijdIds.Tijd2000, EnumValue = 6, DisplayName = "20:00", IsNone = false, IsAvailableOnEersteDag = true, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = false, Order = 6 },
            new StarttijdConfig { Id = StarttijdIds.Tijd2200, EnumValue = 7, DisplayName = "22:00", IsNone = false, IsAvailableOnEersteDag = true, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = false, Order = 7 },
            new StarttijdConfig { Id = StarttijdIds.Tijd0000, EnumValue = 8, DisplayName = "00:00", IsNone = false, IsAvailableOnEersteDag = true, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = false, Order = 8 },
        ];
        return model;
    }

    [Test]
    public void LoadSelectedPlotboekPagina_sets_null_when_no_PaginaId()
    {
        var model = CreateModel();
        model.PlotboekPaginas =
        [
            new PlotboekModel.PlotboekPaginaInfo
            {
                Id = PlotboekPaginaId.Next(),
                Title = "Test",
                Dag = Dag.None,
                StarttijdId = StarttijdIds.None,
                Order = 1,
            },
        ];

        model.LoadSelectedPlotboekPagina();

        model.SelectedPlotboekPagina.Should().BeNull();
    }

    [Test]
    public void LoadSelectedPlotboekPagina_finds_matching_pagina()
    {
        var paginaId = PlotboekPaginaId.Next();
        var model = CreateModel();
        model.PaginaId = paginaId;
        model.PlotboekPaginas =
        [
            new PlotboekModel.PlotboekPaginaInfo
            {
                Id = PlotboekPaginaId.Next(),
                Title = "Other",
                Dag = Dag.None,
                StarttijdId = StarttijdIds.None,
                Order = 1,
            },
            new PlotboekModel.PlotboekPaginaInfo
            {
                Id = paginaId,
                Title = "Selected",
                Dag = Dag.Vrijdag,
                StarttijdId = StarttijdIds.Tijd1030,
                Order = 2,
            },
        ];

        model.LoadSelectedPlotboekPagina();

        model.SelectedPlotboekPagina.Should().NotBeNull();
        model.SelectedPlotboekPagina!.Title.Should().Be("Selected");
    }

    [Test]
    public void LoadSelectedPlotboekPagina_returns_null_when_not_found()
    {
        var model = CreateModel();
        model.PaginaId = PlotboekPaginaId.Next();
        model.PlotboekPaginas =
        [
            new PlotboekModel.PlotboekPaginaInfo
            {
                Id = PlotboekPaginaId.Next(),
                Title = "Other",
                Dag = Dag.None,
                StarttijdId = StarttijdIds.None,
                Order = 1,
            },
        ];

        model.LoadSelectedPlotboekPagina();

        model.SelectedPlotboekPagina.Should().BeNull();
    }

    [Test]
    public void LoadAvailableStarttijden_returns_only_None_when_no_selected_pagina()
    {
        var model = CreateModel();
        model.SelectedPlotboekPagina = null;

        model.LoadAvailableStarttijden();

        model.AvailableStarttijden.Should().ContainSingle();
        model.AvailableStarttijden[0].Should().Be(StarttijdIds.None);
    }

    [Test]
    public void LoadAvailableStarttijden_returns_only_None_when_no_plotboek_settings()
    {
        var model = CreateModel();
        model.SelectedPlotboekPagina = new PlotboekModel.PlotboekPaginaInfo
        {
            Id = PlotboekPaginaId.Next(),
            Title = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = StarttijdIds.None,
            Order = 1,
        };
        model.PlotboekSettings = null;

        model.LoadAvailableStarttijden();

        model.AvailableStarttijden.Should().ContainSingle();
        model.AvailableStarttijden[0].Should().Be(StarttijdIds.None);
    }

    [Test]
    public void LoadAvailableStarttijden_delegates_to_GetAvailableStarttijden()
    {
        var model = CreateModel();
        model.SelectedPlotboekPagina = new PlotboekModel.PlotboekPaginaInfo
        {
            Id = PlotboekPaginaId.Next(),
            Title = "Test",
            Dag = Dag.Zaterdag,
            StarttijdId = StarttijdIds.None,
            Order = 1,
        };
        model.PlotboekSettings = new PlotboekModel.PlotboekSettingsInfo
        {
            StartDag = Dag.Vrijdag,
            EindDag = Dag.Zondag,
        };

        model.LoadAvailableStarttijden();

        model.AvailableStarttijden.Should().HaveCountGreaterThan(1);
        model.AvailableStarttijden.Should().Contain(StarttijdIds.Tijd1030);
        model.AvailableStarttijden.Should().Contain(StarttijdIds.Tijd2000);
    }

    [Test]
    public void LoadAvailableStarttijden_first_day_gets_evening_times()
    {
        var model = CreateModel();
        model.SelectedPlotboekPagina = new PlotboekModel.PlotboekPaginaInfo
        {
            Id = PlotboekPaginaId.Next(),
            Title = "Test",
            Dag = Dag.Vrijdag,
            StarttijdId = StarttijdIds.None,
            Order = 1,
        };
        model.PlotboekSettings = new PlotboekModel.PlotboekSettingsInfo
        {
            StartDag = Dag.Vrijdag,
            EindDag = Dag.Zondag,
        };

        model.LoadAvailableStarttijden();

        model.AvailableStarttijden.Should().Contain(StarttijdIds.Tijd2000);
        model.AvailableStarttijden.Should().Contain(StarttijdIds.Tijd2200);
        model.AvailableStarttijden.Should().NotContain(StarttijdIds.Tijd1030);
    }

    [Test]
    public void LoadAvailableStarttijden_last_day_gets_morning_times()
    {
        var model = CreateModel();
        model.SelectedPlotboekPagina = new PlotboekModel.PlotboekPaginaInfo
        {
            Id = PlotboekPaginaId.Next(),
            Title = "Test",
            Dag = Dag.Zondag,
            StarttijdId = StarttijdIds.None,
            Order = 1,
        };
        model.PlotboekSettings = new PlotboekModel.PlotboekSettingsInfo
        {
            StartDag = Dag.Vrijdag,
            EindDag = Dag.Zondag,
        };

        model.LoadAvailableStarttijden();

        model.AvailableStarttijden.Should().Contain(StarttijdIds.Tijd1030);
        model.AvailableStarttijden.Should().Contain(StarttijdIds.Tijd1230);
        model.AvailableStarttijden.Should().NotContain(StarttijdIds.Tijd2000);
    }
}

