namespace PlotboekGenerator.test.Pages;

public sealed class PlotboekModel_BuildBijlagenData_specs
{
    private static PlotboekModel CreateModel()
        => new (null!, null!, null!, null!, null!, null!, null!, null!, null!);

    [Test]
    public void Maps_bijlage_and_preserves_existing_id()
    {
        var id = BijlageId.Next();
        var model = CreateModel();
        model.EditableBijlagen =
        [
            new PlotboekModel.BijlageInput { Id = id.ToString(), Naam = "Prijslijst", InhoudHtml = "<p>x</p>" },
        ];

        var result = model.BuildBijlagenData();

        result.Bijlagen.Should().ContainSingle();
        result.Bijlagen[0].Id.Should().Be(id);
        result.Bijlagen[0].Titel.Should().Be("Prijslijst");
        result.Bijlagen[0].Inhoud.Should().Be("<p>x</p>");
    }

    [Test]
    public void Mints_an_id_for_a_new_bijlage()
    {
        var model = CreateModel();
        model.EditableBijlagen =
        [
            new PlotboekModel.BijlageInput { Id = null, Naam = "Nieuw", InhoudHtml = "<p>y</p>" },
        ];

        var result = model.BuildBijlagenData();

        result.Bijlagen.Should().ContainSingle();
        result.Bijlagen[0].Id.HasValue.Should().BeTrue();
    }

    [Test]
    public void Drops_bijlagen_that_have_neither_a_name_nor_content()
    {
        var model = CreateModel();
        model.EditableBijlagen =
        [
            new PlotboekModel.BijlageInput { Naam = "  ", InhoudHtml = string.Empty },
            new PlotboekModel.BijlageInput { Naam = "Statblock", InhoudHtml = "<p>y</p>" },
        ];

        var result = model.BuildBijlagenData();

        result.Bijlagen.Should().ContainSingle();
        result.Bijlagen[0].Titel.Should().Be("Statblock");
    }

    [TestCase("<p><br></p>", TestName = "Drops_a_row_whose_editor_was_opened_but_never_typed_in")]
    [TestCase("<div class=\"ql-editor ql-blank\" contenteditable=\"true\"><p><br></p></div>", TestName = "Drops_a_row_holding_only_empty_quill_wrapper_markup")]
    public void Drops_nameless_rows_whose_content_is_only_empty_editor_markup(string inhoudHtml)
    {
        var model = CreateModel();
        model.EditableBijlagen =
        [
            new PlotboekModel.BijlageInput { Naam = string.Empty, InhoudHtml = inhoudHtml },
        ];

        var result = model.BuildBijlagenData();

        result.Bijlagen.Should().BeEmpty("markup left behind by an untouched editor is not content the user typed");
    }

    [Test]
    public void Keeps_a_nameless_bijlage_that_has_content_so_it_is_never_discarded_unnoticed()
    {
        var model = CreateModel();
        model.EditableBijlagen =
        [
            new PlotboekModel.BijlageInput { Naam = "  ", InhoudHtml = "<p>getypte tekst</p>" },
            new PlotboekModel.BijlageInput { Naam = "Statblock", InhoudHtml = "<p>y</p>" },
        ];

        var result = model.BuildBijlagenData();

        result.Bijlagen.Should().HaveCount(2, "dropping it here is what silently lost the user's typing");
        result.Bijlagen[0].Inhoud.Should().Be("<p>getypte tekst</p>");
    }

    [Test]
    public void Assigns_order_by_position()
    {
        var model = CreateModel();
        model.EditableBijlagen =
        [
            new PlotboekModel.BijlageInput { Naam = "Eerste", InhoudHtml = "<p>a</p>" },
            new PlotboekModel.BijlageInput { Naam = "Tweede", InhoudHtml = "<p>b</p>" },
        ];

        var result = model.BuildBijlagenData();

        result.Bijlagen[0].Order.Should().Be(0);
        result.Bijlagen[1].Order.Should().Be(1);
    }
}
