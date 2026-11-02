namespace PlotboekGenerator.test.Pages;

/// <summary>
/// The derived key is written once, on creation, and stored plots reference it forever. These cases pin
/// the shape of that key so a future tweak cannot silently start producing different ids.
/// </summary>
public sealed class AdminModel_DeriveStarttijdId_specs
{
    [TestCase("11:30", "Tijd1130")]
    [TestCase("9:30", "Tijd0930")]
    [TestCase("00:00", "Tijd0000")]
    [TestCase("13:30: Eindscène", "Tijd1330")]
    [TestCase("19:30: Introquest", "Tijd1930")]
    [TestCase("  10:30  ", "Tijd1030")]
    public void Derives_the_key_from_the_time_the_name_starts_with(string naam, string expected)
    {
        AdminModel.DeriveStarttijdId(naam).Should().Be(expected);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    [TestCase("Ochtend")]
    [TestCase("Blok om 11:30")]
    public void Returns_empty_when_the_name_does_not_start_with_a_time(string naam)
    {
        AdminModel.DeriveStarttijdId(naam).Should().BeEmpty(
            because: "the order and the time schedule both read the time back out of the name");
    }

    [TestCase("24:00")]
    [TestCase("25:30")]
    [TestCase("10:60")]
    [TestCase("10:99")]
    public void Returns_empty_for_a_time_that_does_not_exist(string naam)
    {
        AdminModel.DeriveStarttijdId(naam).Should().BeEmpty();
    }
}
