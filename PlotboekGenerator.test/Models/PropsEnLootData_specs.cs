namespace PlotboekGenerator.test.Models;

public sealed class PropsEnLootData_specs
{
    [Test]
    public void Empty_has_no_value()
    {
        PropsEnLootData.Empty.HasValue.Should().BeFalse();
    }

    [Test]
    public void HasValue_when_props_exist()
    {
        var data = PropsEnLootData.Create(
            [PropLootItemData.Create("Zwaard")],
            []);
        data.HasValue.Should().BeTrue();
    }

    [Test]
    public void HasValue_when_loot_exist()
    {
        var data = PropsEnLootData.Create(
            [],
            [PropLootItemData.Create("Goud")]);
        data.HasValue.Should().BeTrue();
    }

    [Test]
    public void HasValue_is_false_when_only_empty_items()
    {
        var data = PropsEnLootData.Create(
            [PropLootItemData.Empty],
            [PropLootItemData.Empty]);
        data.HasValue.Should().BeFalse();
    }

    [Test]
    public void Create_filters_out_empty_items()
    {
        var data = PropsEnLootData.Create(
            [PropLootItemData.Create("Zwaard"), PropLootItemData.Empty],
            [PropLootItemData.Empty, PropLootItemData.Create("Goud")]);
        data.Props.Should().ContainSingle();
        data.Loot.Should().ContainSingle();
    }

    [Test]
    public void Create_with_null_returns_empty_lists()
    {
        var data = PropsEnLootData.Create(null, null);
        data.Props.Should().BeEmpty();
        data.Loot.Should().BeEmpty();
    }

    [Test]
    public void ToPropsString_empty_returns_empty()
    {
        PropsEnLootData.Empty.ToPropsString().Should().BeEmpty();
    }

    [Test]
    public void ToPropsString_single_item()
    {
        var data = PropsEnLootData.Create(
            [PropLootItemData.Create("Zwaard", null, "Jan", "SL", null)],
            []);
        data.ToPropsString().Should().Be("Zwaard (Jan)");
    }

    [Test]
    public void ToPropsString_multiple_items_joined_with_newline()
    {
        var data = PropsEnLootData.Create(
            [PropLootItemData.Create("Zwaard"), PropLootItemData.Create("Schild")],
            []);
        data.ToPropsString().Should().Be("Zwaard\nSchild");
    }

    [Test]
    public void ToLootString_empty_returns_empty()
    {
        PropsEnLootData.Empty.ToLootString().Should().BeEmpty();
    }

    [Test]
    public void ToLootString_multiple_items()
    {
        var data = PropsEnLootData.Create(
            [],
            [PropLootItemData.Create("Goud"), PropLootItemData.Create("Diamant")]);
        data.ToLootString().Should().Be("Goud\nDiamant");
    }

    [Test]
    public void Equals_same_data()
    {
        var a = PropsEnLootData.Create(
            [PropLootItemData.Create("Zwaard")],
            [PropLootItemData.Create("Goud")]);
        var b = PropsEnLootData.Create(
            [PropLootItemData.Create("Zwaard")],
            [PropLootItemData.Create("Goud")]);
        a.Should().Be(b);
    }

    [Test]
    public void Not_equals_different_props()
    {
        var a = PropsEnLootData.Create([PropLootItemData.Create("Zwaard")], []);
        var b = PropsEnLootData.Create([PropLootItemData.Create("Schild")], []);
        a.Should().NotBe(b);
    }

    [Test]
    public void Two_empties_are_equal()
    {
        PropsEnLootData.Empty.Should().Be(PropsEnLootData.Empty);
    }
}

