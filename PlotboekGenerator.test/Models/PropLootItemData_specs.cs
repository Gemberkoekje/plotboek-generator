namespace PlotboekGenerator.test.Models;

public sealed class PropLootItemData_specs
{
    [Test]
    public void Empty_has_no_value()
    {
        PropLootItemData.Empty.HasValue.Should().BeFalse();
    }

    [Test]
    public void HasValue_when_omschrijving_is_set()
    {
        var data = PropLootItemData.Create("Zwaard");
        data.HasValue.Should().BeTrue();
    }

    [Test]
    public void HasValue_is_false_when_omschrijving_is_whitespace()
    {
        var data = PropLootItemData.Create("  ");
        data.HasValue.Should().BeFalse();
    }

    [Test]
    public void Create_with_null_omschrijving_uses_empty_string()
    {
        var data = PropLootItemData.Create(null);
        data.Omschrijving.Should().BeEmpty();
    }

    [Test]
    public void ToString_empty_returns_empty_string()
    {
        PropLootItemData.Empty.ToString().Should().BeEmpty();
    }

    [Test]
    public void ToString_without_verantwoordelijke()
    {
        var data = PropLootItemData.Create("Zwaard");
        data.ToString().Should().Be("Zwaard");
    }

    [Test]
    public void ToString_with_verantwoordelijke_name()
    {
        var data = PropLootItemData.Create("Zwaard", "sl-1", "Jan", "SL", null);
        data.ToString().Should().Be("Zwaard (Jan)");
    }

    [Test]
    public void ToString_with_verantwoordelijke_id_but_no_name()
    {
        var data = PropLootItemData.Create("Zwaard", "sl-1", null, "SL", null);
        data.ToString().Should().Be("Zwaard");
    }

    [Test]
    public void ToString_with_verantwoordelijke_vrije_tekst()
    {
        var data = PropLootItemData.Create("Zwaard", null, null, "Anders", "Speler X");
        data.ToString().Should().Be("Zwaard (Speler X)");
    }
}

