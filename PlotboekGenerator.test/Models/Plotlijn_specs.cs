namespace PlotboekGenerator.test.Models;

public sealed class Plotlijn_specs
{
    private static PlotboekId CreatePlotboekId() => PlotboekId.Create(Qowaiv.Year.Create(2025), EventIds.Voorjaar);

    [Test]
    public void Titel_empty_when_no_paginas()
    {
        var plotlijn = new Plotlijn
        {
            PlotlijnId = PlotlijnId.Next(),
            PlotboekId = CreatePlotboekId(),
            DomainId = Guid.NewGuid(),
            DomainShorthand = "Ti",
            PaginaTitels = [],
        };

        plotlijn.Titel.Should().BeEmpty();
    }

    [Test]
    public void Titel_single_pagina()
    {
        var plotlijn = new Plotlijn
        {
            PlotlijnId = PlotlijnId.Next(),
            PlotboekId = CreatePlotboekId(),
            DomainId = Guid.NewGuid(),
            DomainShorthand = "Ti",
            PaginaTitels = ["De Verloren Tempel"],
        };

        plotlijn.Titel.Should().Be("De Verloren Tempel");
    }

    [Test]
    public void Titel_two_paginas()
    {
        var plotlijn = new Plotlijn
        {
            PlotlijnId = PlotlijnId.Next(),
            PlotboekId = CreatePlotboekId(),
            DomainId = Guid.NewGuid(),
            DomainShorthand = "Ti",
            PaginaTitels = ["Plot A", "Plot B"],
        };

        plotlijn.Titel.Should().Be("Plot A; Plot B");
    }

    [Test]
    public void Titel_three_paginas()
    {
        var plotlijn = new Plotlijn
        {
            PlotlijnId = PlotlijnId.Next(),
            PlotboekId = CreatePlotboekId(),
            DomainId = Guid.NewGuid(),
            DomainShorthand = "Ti",
            PaginaTitels = ["Plot A", "Plot B", "Plot C"],
        };

        plotlijn.Titel.Should().Be("Plot A; Plot B; Plot C");
    }

    [Test]
    public void Titel_four_or_more_paginas_shows_first_with_count()
    {
        var plotlijn = new Plotlijn
        {
            PlotlijnId = PlotlijnId.Next(),
            PlotboekId = CreatePlotboekId(),
            DomainId = Guid.NewGuid(),
            DomainShorthand = "Ti",
            PaginaTitels = ["Plot A", "Plot B", "Plot C", "Plot D"],
        };

        plotlijn.Titel.Should().Be("Plot A (+3 plots)");
    }

    [Test]
    public void Titel_five_paginas()
    {
        var plotlijn = new Plotlijn
        {
            PlotlijnId = PlotlijnId.Next(),
            PlotboekId = CreatePlotboekId(),
            DomainId = Guid.NewGuid(),
            DomainShorthand = "Ti",
            PaginaTitels = ["Eerste", "Tweede", "Derde", "Vierde", "Vijfde"],
        };

        plotlijn.Titel.Should().Be("Eerste (+4 plots)");
    }

    [Test]
    public void Id_is_string_of_plotlijn_id()
    {
        var plotlijnId = PlotlijnId.Next();
        var plotlijn = new Plotlijn
        {
            PlotlijnId = plotlijnId,
            PlotboekId = CreatePlotboekId(),
            DomainId = Guid.NewGuid(),
            DomainShorthand = "Ti",
        };

        plotlijn.Id.Should().Be(plotlijnId.ToString());
    }

    [Test]
    public void PlotboekIdQueryable_is_string_of_plotboek_id()
    {
        var plotboekId = CreatePlotboekId();
        var plotlijn = new Plotlijn
        {
            PlotlijnId = PlotlijnId.Next(),
            PlotboekId = plotboekId,
            DomainId = Guid.NewGuid(),
            DomainShorthand = "Ti",
        };

        plotlijn.PlotboekIdQueryable.Should().Be(plotboekId.ToString());
    }
}
