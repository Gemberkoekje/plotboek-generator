namespace PlotboekGenerator.test.Models;

public sealed class StandaardNpc_specs
{
    private static readonly DateTime Moment = new (2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);

    [TestCase("Stadswacht")]
    [TestCase(" stadswacht ")]
    [TestCase("STADSWACHT")]
    [TestCase("Stadswächt")]
    public void The_key_ignores_case_spaces_and_diacritics(string rol)
    {
        StandaardNpc.IdVoor(rol).Should().Be(
            "Stadswacht",
            "an SL typing the name slightly differently must hit the same standaard-NPC, not make a second one");
    }

    [Test]
    public void The_key_glues_the_words_of_a_longer_name()
    {
        StandaardNpc.IdVoor("Zwarte wacht").Should().Be("ZwarteWacht");
    }

    [TestCase("")]
    [TestCase("!!!")]
    public void A_name_without_letters_or_digits_has_no_key(string rol)
    {
        StandaardNpc.IdVoor(rol).Should().BeEmpty();
    }

    [Test]
    public void Create_trims_every_text()
    {
        var standaardNpc = StandaardNpc.Create("  Stadswacht ", " tabbaard, helm ", " stevig postuur ", "anna", Moment);

        standaardNpc.Should().BeEquivalentTo(new
        {
            Id = "Stadswacht",
            Rol = "Stadswacht",
            KledingEnSchmink = "tabbaard, helm",
            CastingInstructie = "stevig postuur",
            GewijzigdDoor = "anna",
            GewijzigdOp = Moment,
        });
    }

    [Test]
    public void Create_turns_missing_texts_into_empty_strings()
    {
        var standaardNpc = StandaardNpc.Create("Bandiet", null!, null!, null!, Moment);

        standaardNpc.KledingEnSchmink.Should().BeEmpty();
        standaardNpc.CastingInstructie.Should().BeEmpty();
        standaardNpc.GewijzigdDoor.Should().BeEmpty();
    }

    [Test]
    public void Create_refuses_a_name_without_a_key()
    {
        var create = () => StandaardNpc.Create("!!!", string.Empty, string.Empty, "anna", Moment);

        create.Should().Throw<ArgumentException>(because: "handlers validate the name first, so this is a programmer error");
    }
}
