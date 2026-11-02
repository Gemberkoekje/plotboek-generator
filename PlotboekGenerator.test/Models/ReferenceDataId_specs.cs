namespace PlotboekGenerator.test.Models;

/// <summary>
/// The derived key is written once, on creation, and stored plots reference it forever. These cases pin
/// the shape of that key so a future tweak cannot silently start producing different ids.
/// </summary>
public sealed class ReferenceDataId_specs
{
    [TestCase("Leeuwenkuil", "Leeuwenkuil")]
    [TestCase("Jongensveld voor", "JongensveldVoor")]
    [TestCase("Oud-gedienden bos", "OudGediendenBos")]
    [TestCase("Ruïne", "Ruine")]
    [TestCase("Route langs Henry's Hoekje tot aan Leeuwenkuil", "RouteLangsHenrysHoekjeTotAanLeeuwenkuil")]
    [TestCase("Tommesbos (alle velden)", "TommesbosAlleVelden")]
    [TestCase("  Heilige   hoek  ", "HeiligeHoek")]
    [TestCase("Veld 2", "Veld2")]
    public void Derives_a_pascal_case_key_without_diacritics(string naam, string expected)
    {
        ReferenceDataId.Derive(naam).Should().Be(expected);
    }

    [Test]
    public void Prefixes_a_key_that_would_start_with_a_digit()
    {
        ReferenceDataId.Derive("2e Sportveld").Should().Be(
            "N2eSportveld",
            "an identifier starting with a digit is awkward to work with everywhere else");
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("!!!")]
    public void Returns_empty_when_nothing_usable_is_left(string naam)
    {
        ReferenceDataId.Derive(naam).Should().BeEmpty();
    }

    [Test]
    public void Returns_empty_for_no_name_at_all()
    {
        ReferenceDataId.Derive(null!).Should().BeEmpty();
    }
}
