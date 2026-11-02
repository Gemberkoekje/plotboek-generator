namespace PlotboekGenerator.test.Models;

public sealed class PlotboekPaginaExportData_specs
{
    [Test]
    public void DisplayTitle_with_domain_shorthand()
    {
        var data = new PlotboekPagina.ExportData
        {
            Titel = "Hier komt de handelaar",
            DomainShorthand = "Ti",
        };

        data.DisplayTitle.Should().Be("Ti - Hier komt de handelaar");
    }

    [Test]
    public void DisplayTitle_without_domain_shorthand()
    {
        var data = new PlotboekPagina.ExportData
        {
            Titel = "Losstaand plot",
            DomainShorthand = null,
        };

        data.DisplayTitle.Should().Be("Losstaand plot");
    }

    [Test]
    public void DisplayTitle_with_empty_domain_shorthand()
    {
        var data = new PlotboekPagina.ExportData
        {
            Titel = "Test Encounter",
            DomainShorthand = string.Empty,
        };

        data.DisplayTitle.Should().Be("Test Encounter");
    }

    [Test]
    public void Id_is_string_of_pagina_id()
    {
        var paginaId = PlotboekPaginaId.Next();
        var data = new PlotboekPagina.ExportData
        {
            PaginaId = paginaId,
        };

        data.Id.Should().Be(paginaId.ToString());
    }

    [Test]
    public void PlotboekIdQueryable_is_string_of_plotboek_id()
    {
        var plotboekId = PlotboekId.Create(Qowaiv.Year.Create(2025), EventIds.Voorjaar);
        var data = new PlotboekPagina.ExportData
        {
            PlotboekId = plotboekId,
        };

        data.PlotboekIdQueryable.Should().Be(plotboekId.ToString());
    }

    [Test]
    public void PlotlijnIdQueryable_is_string_of_plotlijn_id()
    {
        var plotlijnId = PlotlijnId.Next();
        var data = new PlotboekPagina.ExportData
        {
            PlotlijnId = plotlijnId,
        };

        data.PlotlijnIdQueryable.Should().Be(plotlijnId.ToString());
    }
}
