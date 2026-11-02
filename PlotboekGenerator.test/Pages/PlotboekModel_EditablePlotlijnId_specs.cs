namespace PlotboekGenerator.test.Pages;

public sealed class PlotboekModel_EditablePlotlijnId_specs
{
    private static PlotboekModel CreateModel()
        => new (null!, null!, null!, null!, null!, null!, null!, null!, null!);

    [Test]
    public void Returns_empty_when_raw_is_null()
    {
        var model = CreateModel();
        model.EditablePlotlijnIdRaw = null;

        model.EditablePlotlijnId.Should().Be(PlotlijnId.Empty);
    }

    [Test]
    public void Returns_empty_when_raw_is_empty()
    {
        var model = CreateModel();
        model.EditablePlotlijnIdRaw = string.Empty;

        model.EditablePlotlijnId.Should().Be(PlotlijnId.Empty);
    }

    [Test]
    public void Returns_empty_when_raw_has_pagina_prefix()
    {
        var model = CreateModel();
        model.EditablePlotlijnIdRaw = "pagina:some-id";

        model.EditablePlotlijnId.Should().Be(PlotlijnId.Empty);
    }

    [Test]
    public void Returns_empty_when_raw_has_unknown_prefix()
    {
        var model = CreateModel();
        model.EditablePlotlijnIdRaw = "unknown:some-id";

        model.EditablePlotlijnId.Should().Be(PlotlijnId.Empty);
    }

    [Test]
    public void Parses_plotlijn_prefix()
    {
        var id = PlotlijnId.Next();
        var model = CreateModel();
        model.EditablePlotlijnIdRaw = $"plotlijn:{id}";

        model.EditablePlotlijnId.Should().Be(id);
    }

    [Test]
    public void Parses_plotlijn_prefix_case_insensitive()
    {
        var id = PlotlijnId.Next();
        var model = CreateModel();
        model.EditablePlotlijnIdRaw = $"Plotlijn:{id}";

        model.EditablePlotlijnId.Should().Be(id);
    }
}

