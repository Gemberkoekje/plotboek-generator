namespace PlotboekGenerator.test.Models;

public sealed class GeschatteTijdsduur_specs
{
    [Test]
    public void Empty_has_no_value()
    {
        GeschatteTijdsduur.Empty.HasValue.Should().BeFalse();
    }

    [Test]
    public void HasValue_when_minimum_is_set()
    {
        var data = GeschatteTijdsduur.Create(30);
        data.HasValue.Should().BeTrue();
    }

    [Test]
    public void IsRange_when_minimum_differs_from_maximum()
    {
        var data = GeschatteTijdsduur.Create(30, 60);
        data.IsRange.Should().BeTrue();
    }

    [Test]
    public void IsRange_is_false_when_minimum_equals_maximum()
    {
        var data = GeschatteTijdsduur.Create(30, 30);
        data.IsRange.Should().BeFalse();
    }

    [Test]
    public void IsRange_is_false_when_no_maximum()
    {
        var data = GeschatteTijdsduur.Create(30);
        data.IsRange.Should().BeFalse();
    }

    [Test]
    public void ToString_empty_returns_empty_string()
    {
        GeschatteTijdsduur.Empty.ToString().Should().BeEmpty();
    }

    [Test]
    public void ToString_minutes_only()
    {
        var data = GeschatteTijdsduur.Create(45);
        data.ToString().Should().Be("45 minuten");
    }

    [Test]
    public void ToString_exactly_one_hour()
    {
        var data = GeschatteTijdsduur.Create(60);
        data.ToString().Should().Be("1 uur");
    }

    [Test]
    public void ToString_multiple_hours()
    {
        var data = GeschatteTijdsduur.Create(120);
        data.ToString().Should().Be("2 uur");
    }

    [Test]
    public void ToString_hours_and_minutes()
    {
        var data = GeschatteTijdsduur.Create(90);
        data.ToString().Should().Be("1 uur en 30 minuten");
    }

    [Test]
    public void ToString_two_hours_and_minutes()
    {
        var data = GeschatteTijdsduur.Create(150);
        data.ToString().Should().Be("2 uur en 30 minuten");
    }

    [Test]
    public void ToString_range()
    {
        var data = GeschatteTijdsduur.Create(30, 60);
        data.ToString().Should().Be("30 minuten - 1 uur");
    }

    [Test]
    public void ToString_range_hours_and_minutes()
    {
        var data = GeschatteTijdsduur.Create(45, 90);
        data.ToString().Should().Be("45 minuten - 1 uur en 30 minuten");
    }

    [Test]
    public void ToString_only_maximum_set_returns_empty_because_no_minimum()
    {
        var data = GeschatteTijdsduur.Create(null, 60);
        data.HasValue.Should().BeFalse();
        data.ToString().Should().BeEmpty();
    }

    [Test]
    public void ToString_exact_when_minimum_equals_maximum()
    {
        var data = GeschatteTijdsduur.Create(60, 60);
        data.ToString().Should().Be("1 uur");
    }

    [Test]
    public void ToString_zero_minutes_returns_empty()
    {
        var data = GeschatteTijdsduur.Create(0);
        data.ToString().Should().BeEmpty();
    }
}

