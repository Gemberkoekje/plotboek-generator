namespace PlotboekGenerator.test.Models;

public sealed class EncounterTypeIds_specs
{
    [Test]
    public void Empty_has_no_value()
    {
        EncounterTypeIds.Empty.HasValue.Should().BeFalse();
    }

    [Test]
    public void HasValue_is_true_when_major_types_exist()
    {
        var types = EncounterTypeIds.Create([SoortEncounterIds.Vechten]);

        types.HasValue.Should().BeTrue();
    }

    [Test]
    public void HasValue_is_false_when_only_minor_types_exist()
    {
        var types = EncounterTypeIds.Create([], [SoortEncounterIds.Roleplay]);

        types.HasValue.Should().BeFalse();
    }

    [Test]
    public void Create_filters_out_None()
    {
        var types = EncounterTypeIds.Create(
            [SoortEncounterIds.None, SoortEncounterIds.Vechten],
            [SoortEncounterIds.None, SoortEncounterIds.Roleplay]);

        types.Major.Should().BeEquivalentTo([SoortEncounterIds.Vechten]);
        types.Minor.Should().BeEquivalentTo([SoortEncounterIds.Roleplay]);
    }

    [Test]
    public void Create_with_null_major_returns_empty_lists()
    {
        var types = EncounterTypeIds.Create(null);

        types.Major.Should().BeEmpty();
        types.Minor.Should().BeEmpty();
    }

    [Test]
    public void ToString_empty_returns_empty_string()
    {
        EncounterTypeIds.Empty.ToString().Should().BeEmpty();
    }

    [Test]
    public void ToString_single_major()
    {
        var types = EncounterTypeIds.Create([SoortEncounterIds.Vechten]);

        types.ToString().Should().Be(SoortEncounterIds.Vechten);
    }

    [Test]
    public void ToString_multiple_major_types_joined_with_slash()
    {
        var types = EncounterTypeIds.Create([SoortEncounterIds.Roleplay, SoortEncounterIds.Handel]);

        types.ToString().Should().Be("Roleplay / Handel");
    }

    [Test]
    public void ToString_with_minor_types()
    {
        var types = EncounterTypeIds.Create(
            [SoortEncounterIds.Vechten],
            [SoortEncounterIds.Roleplay]);

        types.ToString().Should().Be("Vechten (mogelijk Roleplay)");
    }

    [Test]
    public void ToString_with_multiple_minor_types()
    {
        var types = EncounterTypeIds.Create(
            [SoortEncounterIds.Vechten],
            [SoortEncounterIds.Roleplay, SoortEncounterIds.Magie]);

        types.ToString().Should().Be("Vechten (mogelijk Roleplay, Magie)");
    }

    [Test]
    public void Equals_same_types()
    {
        var a = EncounterTypeIds.Create([SoortEncounterIds.Vechten], [SoortEncounterIds.Roleplay]);
        var b = EncounterTypeIds.Create([SoortEncounterIds.Vechten], [SoortEncounterIds.Roleplay]);

        a.Should().Be(b);
    }

    [Test]
    public void Not_equals_different_major()
    {
        var a = EncounterTypeIds.Create([SoortEncounterIds.Vechten]);
        var b = EncounterTypeIds.Create([SoortEncounterIds.Roleplay]);

        a.Should().NotBe(b);
    }

    [Test]
    public void Not_equals_different_minor()
    {
        var a = EncounterTypeIds.Create([SoortEncounterIds.Vechten], [SoortEncounterIds.Roleplay]);
        var b = EncounterTypeIds.Create([SoortEncounterIds.Vechten], [SoortEncounterIds.Magie]);

        a.Should().NotBe(b);
    }

    [Test]
    public void Two_empties_are_equal()
    {
        EncounterTypeIds.Empty.Should().Be(EncounterTypeIds.Empty);
    }
}
