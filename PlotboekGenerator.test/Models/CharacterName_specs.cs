namespace PlotboekGenerator.test.Models;

public sealed class CharacterName_specs
{
    [Test]
    public void Create_sets_naam()
    {
        var character = CharacterName.Create("Aldric de Moedige");

        character.Naam.Should().Be("Aldric de Moedige");
    }

    [Test]
    public void Create_generates_unique_ids()
    {
        var a = CharacterName.Create("Aldric");
        var b = CharacterName.Create("Aldric");

        a.Id.Should().NotBe(b.Id);
    }
}

