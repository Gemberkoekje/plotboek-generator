namespace PlotboekGenerator.test.Models;

public sealed class WorldName_specs
{
    [Test]
    public void Create_sets_naam()
    {
        var world = WorldName.Create("Tiartal");

        world.Naam.Should().Be("Tiartal");
    }

    [Test]
    public void Create_generates_unique_ids()
    {
        var a = WorldName.Create("Tiartal");
        var b = WorldName.Create("Tiartal");

        a.Id.Should().NotBe(b.Id);
    }
}

