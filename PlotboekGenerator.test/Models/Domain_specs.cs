namespace PlotboekGenerator.test.Models;

public sealed class Domain_specs
{
    [Test]
    public void Create_sets_shorthand()
    {
        var domain = Domain.Create("Ti", "Tiartal", 1);

        domain.Shorthand.Should().Be("Ti");
    }

    [Test]
    public void Create_sets_description()
    {
        var domain = Domain.Create("G", "Gildes", 2);

        domain.Description.Should().Be("Gildes");
    }

    [Test]
    public void Create_sets_order()
    {
        var domain = Domain.Create("C", "Chaos", 3);

        domain.Order.Should().Be(3);
    }

    [Test]
    public void Create_generates_unique_ids()
    {
        var a = Domain.Create("Ti", "Tiartal", 1);
        var b = Domain.Create("Ti", "Tiartal", 1);

        a.Id.Should().NotBe(b.Id);
    }

    [Test]
    public void DisplayName_combines_shorthand_and_description()
    {
        var domain = Domain.Create("Ti", "Tiartal", 1);

        domain.DisplayName.Should().Be("Ti - Tiartal");
    }
}

