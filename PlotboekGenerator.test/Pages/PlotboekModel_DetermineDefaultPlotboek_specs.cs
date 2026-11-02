namespace PlotboekGenerator.test.Pages;

public sealed class PlotboekModel_DetermineDefaultPlotboek_specs
{
    private static PlotboekModel CreateModel()
        => new (null!, null!, null!, null!, null!, null!, null!, null!, null!);

    [Test]
    public void Returns_null_when_no_candidates()
    {
        var model = CreateModel();
        model.AvailablePlotboeken =
        [
            new PlotboekModel.PlotboekInfo
            {
                Id = PlotboekId.CreateEvergreen(),
                Jaar = Year.Create(1),
                EventId = "Evergreen",
            },
        ];

        model.DetermineDefaultPlotboek().Should().BeNull();
    }

    [Test]
    public void Returns_first_candidate_when_no_existing_plotboeken()
    {
        var model = CreateModel();
        var expected = new PlotboekModel.PlotboekInfo
        {
            Id = PlotboekId.Create(Year.Create(2026), "Voorjaar"),
            Jaar = Year.Create(2026),
            EventId = "Voorjaar",
            Exists = false,
        };
        model.AvailablePlotboeken = [expected];

        model.DetermineDefaultPlotboek().Should().Be(expected);
    }

    [Test]
    public void Returns_active_plotboek_with_future_end_date()
    {
        using var timeScope = Clock.SetTimeForCurrentContext(() => new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        var model = CreateModel();
        var past = new PlotboekModel.PlotboekInfo
        {
            Id = PlotboekId.Create(Year.Create(2025), "Voorjaar"),
            Jaar = Year.Create(2025),
            EventId = "Voorjaar",
            EindDatum = new DateOnly(2025, 3, 1),
            Exists = true,
        };
        var active = new PlotboekModel.PlotboekInfo
        {
            Id = PlotboekId.Create(Year.Create(2025), "Zomer"),
            Jaar = Year.Create(2025),
            EventId = "Zomer",
            EindDatum = new DateOnly(2025, 8, 1),
            Exists = true,
        };
        model.AvailablePlotboeken = [past, active];

        model.DetermineDefaultPlotboek().Should().Be(active);
    }

    [Test]
    public void Returns_plotboek_with_end_date_today()
    {
        using var timeScope = Clock.SetTimeForCurrentContext(() => new DateTime(2025, 8, 1, 0, 0, 0, DateTimeKind.Utc));

        var model = CreateModel();
        var today = new PlotboekModel.PlotboekInfo
        {
            Id = PlotboekId.Create(Year.Create(2025), "Zomer"),
            Jaar = Year.Create(2025),
            EventId = "Zomer",
            EindDatum = new DateOnly(2025, 8, 1),
            Exists = true,
        };
        model.AvailablePlotboeken = [today];

        model.DetermineDefaultPlotboek().Should().Be(today);
    }

    [Test]
    public void Returns_null_when_multiple_plotboeken_without_dates()
    {
        var model = CreateModel();
        model.AvailablePlotboeken =
        [
            new PlotboekModel.PlotboekInfo
            {
                Id = PlotboekId.Create(Year.Create(2025), "Voorjaar"),
                Jaar = Year.Create(2025),
                EventId = "Voorjaar",
                Exists = true,
            },
            new PlotboekModel.PlotboekInfo
            {
                Id = PlotboekId.Create(Year.Create(2025), "Zomer"),
                Jaar = Year.Create(2025),
                EventId = "Zomer",
                Exists = true,
            },
        ];

        model.DetermineDefaultPlotboek().Should().BeNull();
    }

    [Test]
    public void Returns_single_plotboek_without_date()
    {
        using var timeScope = Clock.SetTimeForCurrentContext(() => new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        var model = CreateModel();
        var expected = new PlotboekModel.PlotboekInfo
        {
            Id = PlotboekId.Create(Year.Create(2025), "Voorjaar"),
            Jaar = Year.Create(2025),
            EventId = "Voorjaar",
            Exists = true,
        };
        model.AvailablePlotboeken = [expected];

        model.DetermineDefaultPlotboek().Should().Be(expected);
    }

    [Test]
    public void Returns_last_candidate_when_all_past()
    {
        using var timeScope = Clock.SetTimeForCurrentContext(() => new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var model = CreateModel();
        var past1 = new PlotboekModel.PlotboekInfo
        {
            Id = PlotboekId.Create(Year.Create(2025), "Voorjaar"),
            Jaar = Year.Create(2025),
            EventId = "Voorjaar",
            EindDatum = new DateOnly(2025, 3, 1),
            Exists = true,
        };
        var past2 = new PlotboekModel.PlotboekInfo
        {
            Id = PlotboekId.Create(Year.Create(2025), "Zomer"),
            Jaar = Year.Create(2025),
            EventId = "Zomer",
            EindDatum = new DateOnly(2025, 8, 1),
            Exists = true,
        };
        var next = new PlotboekModel.PlotboekInfo
        {
            Id = PlotboekId.Create(Year.Create(2025), "Najaar"),
            Jaar = Year.Create(2025),
            EventId = "Najaar",
            Exists = false,
        };
        model.AvailablePlotboeken = [past1, past2, next];

        model.DetermineDefaultPlotboek().Should().Be(next);
    }

    [Test]
    public void Ignores_evergreen_candidates()
    {
        var model = CreateModel();
        model.AvailablePlotboeken =
        [
            new PlotboekModel.PlotboekInfo
            {
                Id = PlotboekId.CreateEvergreen(),
                Jaar = Year.Create(1),
                EventId = "Evergreen",
                Exists = true,
            },
        ];

        model.DetermineDefaultPlotboek().Should().BeNull();
    }

    [Test]
    public void Returns_earliest_future_plotboek_when_multiple_active()
    {
        using var timeScope = Clock.SetTimeForCurrentContext(() => new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        var model = CreateModel();
        var later = new PlotboekModel.PlotboekInfo
        {
            Id = PlotboekId.Create(Year.Create(2025), "Najaar"),
            Jaar = Year.Create(2025),
            EventId = "Najaar",
            EindDatum = new DateOnly(2025, 12, 1),
            Exists = true,
        };
        var earlier = new PlotboekModel.PlotboekInfo
        {
            Id = PlotboekId.Create(Year.Create(2025), "Zomer"),
            Jaar = Year.Create(2025),
            EventId = "Zomer",
            EindDatum = new DateOnly(2025, 8, 1),
            Exists = true,
        };
        model.AvailablePlotboeken = [later, earlier];

        model.DetermineDefaultPlotboek().Should().Be(earlier);
    }
}

