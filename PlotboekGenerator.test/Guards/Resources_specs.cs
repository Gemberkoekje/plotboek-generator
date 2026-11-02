using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace PlotboekGenerator.test.Guards;

/// <summary>
/// Every Dutch resource file has an English sibling that says the same things: the same keys, no empty
/// text, and the same placeholders, so a translation can neither go missing nor drop a value.
/// </summary>
public sealed class Resources_specs
{
    private static readonly Regex Placeholder = new (@"\{(\d+)(?:[,:][^}]*)?\}", RegexOptions.None, TimeSpan.FromSeconds(1));

    [Test]
    public void There_are_resource_files()
        => NeutralFiles().Should().NotBeEmpty("the product's text lives in resource files");

    [Test]
    public void Every_english_file_has_the_same_keys_as_its_dutch_one()
    {
        var problems = NeutralFiles().SelectMany(neutral =>
        {
            var dutch = Read(neutral);
            var english = Read(English(neutral));
            return dutch.Keys.Except(english.Keys).Select(key => $"{Path.GetFileName(neutral)}: {key} has no English text")
                .Concat(english.Keys.Except(dutch.Keys).Select(key => $"{Path.GetFileName(neutral)}: {key} has no Dutch text"));
        });

        problems.Should().BeEmpty("every Dutch text needs an English one, and nothing more");
    }

    [Test]
    public void No_text_is_empty()
    {
        var problems = NeutralFiles().SelectMany(neutral => Read(neutral).Concat(Read(English(neutral)))
            .Where(entry => string.IsNullOrWhiteSpace(entry.Value))
            .Select(entry => $"{Path.GetFileName(neutral)}: {entry.Key}"));

        problems.Should().BeEmpty("an empty text shows as nothing on the page");
    }

    [Test]
    public void A_translation_keeps_the_placeholders()
    {
        var problems = NeutralFiles().SelectMany(neutral =>
        {
            var english = Read(English(neutral));
            return Read(neutral)
                .Where(entry => english.ContainsKey(entry.Key))
                .Where(entry => !Placeholders(entry.Value).SetEquals(Placeholders(english[entry.Key])))
                .Select(entry => $"{Path.GetFileName(neutral)}: {entry.Key}");
        });

        problems.Should().BeEmpty("string.Format would drop or fail on a value the other language does not have");
    }

    private static List<string> NeutralFiles()
    {
        var separator = Path.DirectorySeparatorChar;
        return [.. Directory.EnumerateFiles(RepositoryRoot(), "*.resx", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{separator}obj{separator}", StringComparison.Ordinal)
                && !file.Contains($"{separator}bin{separator}", StringComparison.Ordinal))
            .Where(file => Path.GetFileNameWithoutExtension(file).IndexOf('.', StringComparison.Ordinal) < 0)];
    }

    private static HashSet<string> Placeholders(string text)
        => [.. Placeholder.Matches(text).Select(match => match.Groups[1].Value)];

    private static string English(string neutral)
        => Path.ChangeExtension(neutral, null) + ".en.resx";

    private static Dictionary<string, string> Read(string path)
    {
        File.Exists(path).Should().BeTrue("{0} belongs next to its Dutch file", path);

        return XDocument.Load(path).Root!.Elements("data")
            .ToDictionary(data => (string)data.Attribute("name")!, data => (string?)data.Element("value") ?? string.Empty, StringComparer.Ordinal);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "PlotboekGenerator.core")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The product's projects were not found above the test directory.");
    }
}
