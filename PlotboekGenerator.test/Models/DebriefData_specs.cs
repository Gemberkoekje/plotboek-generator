namespace PlotboekGenerator.test.Models;

public sealed class DebriefData_specs
{
    [Test]
    public void Empty_has_no_value()
    {
        DebriefData.Empty.HasValue.Should().BeFalse();
    }

    [Test]
    public void HasValue_is_true_when_debrief_is_gewenst()
    {
        var data = DebriefData.Create(true, []);
        data.HasValue.Should().BeTrue();
    }

    [Test]
    public void HasValue_is_false_when_debrief_is_not_gewenst()
    {
        var data = DebriefData.Create(false, ["Wat vond je ervan?"]);
        data.HasValue.Should().BeFalse();
    }

    [Test]
    public void Create_clears_vragen_when_not_gewenst()
    {
        var data = DebriefData.Create(false, ["Vraag 1", "Vraag 2"]);
        data.Vragen.Should().BeEmpty();
    }

    [Test]
    public void Create_filters_blank_vragen()
    {
        var data = DebriefData.Create(true, ["Vraag 1", string.Empty, "  ", "Vraag 2"]);
        data.Vragen.Should().BeEquivalentTo("Vraag 1", "Vraag 2");
    }

    [Test]
    public void Create_trims_vragen()
    {
        var data = DebriefData.Create(true, ["  Vraag 1  "]);
        data.Vragen.Should().BeEquivalentTo("Vraag 1");
    }

    [Test]
    public void Create_with_null_vragen_returns_empty_list()
    {
        var data = DebriefData.Create(true, null);
        data.Vragen.Should().BeEmpty();
    }

    [Test]
    public void ToDebriefString_returns_empty_when_not_gewenst()
    {
        var data = DebriefData.Create(false, []);
        data.ToDebriefString().Should().BeEmpty();
    }

    [Test]
    public void ToDebriefString_returns_newlines_when_gewenst_without_questions()
    {
        var data = DebriefData.Create(true, []);
        data.ToDebriefString().Should().Be("\n\n\n");
    }

    [Test]
    public void ToDebriefString_formats_questions_with_trailing_newlines()
    {
        var data = DebriefData.Create(true, ["Vraag 1", "Vraag 2"]);
        data.ToDebriefString().Should().Be("Vraag 1:\n\n\n\nVraag 2:\n\n\n");
    }

    [Test]
    public void ToDebriefString_single_question()
    {
        var data = DebriefData.Create(true, ["Hoe ging het?"]);
        data.ToDebriefString().Should().Be("Hoe ging het?:\n\n\n");
    }

    [Test]
    public void Equals_same_data()
    {
        var a = DebriefData.Create(true, ["Vraag 1"]);
        var b = DebriefData.Create(true, ["Vraag 1"]);
        a.Should().Be(b);
    }

    [Test]
    public void Not_equals_different_gewenst()
    {
        var a = DebriefData.Create(true, []);
        var b = DebriefData.Create(false, []);
        a.Should().NotBe(b);
    }

    [Test]
    public void Not_equals_different_vragen()
    {
        var a = DebriefData.Create(true, ["Vraag 1"]);
        var b = DebriefData.Create(true, ["Vraag 2"]);
        a.Should().NotBe(b);
    }

    [Test]
    public void Two_empties_are_equal()
    {
        DebriefData.Empty.Should().Be(DebriefData.Empty);
    }
}

