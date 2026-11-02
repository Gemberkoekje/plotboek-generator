using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;

namespace PlotboekGenerator.test.Guards;

/// <summary>
/// The product knows no deployment: the words, hosts and sign-in of the LARP it was first written
/// for live in that LARP's own host, never in the projects every deployment shares.
/// </summary>
/// <remarks>
/// A few stored names predate the split and cannot change without a data migration, so they are
/// allowed: the <c>InstructiePentagon</c> field and its event, and the JSON names <c>PLIN</c> and
/// <c>KeycloakRole</c> that <see cref="PlotboekUser"/> pins with an attribute.
/// </remarks>
public sealed class ProductGenericity_specs
{
    private static readonly string[] ProductProjects =
    [
        "PlotboekGenerator.core",
        "PlotboekGenerator.application",
        "PlotboekGenerator.infrastructure",
        "PlotboekGenerator.WebApp",
        "PlotboekGenerator.Host",
        "PlotboekGenerator.test",
    ];

    private static readonly string[] TextExtensions = [".cs", ".cshtml", ".js", ".css", ".json", ".csproj", ".svg"];

    /// <summary>The stored names that may keep their old words.</summary>
    /// <remarks>The JSON names are allowed as quoted strings: that is how they are pinned and checked.</remarks>
    private static readonly string[] FrozenNames = ["InstructiePentagon", "\"PLIN\"", "\"KeycloakRole\""];

    private static readonly Regex DeploymentWords = new(
        @"vortex|pentagon|\bplin\b|keycloak|walrick",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    [Test]
    public void No_product_file_names_the_first_deployment()
    {
        var root = RepositoryRoot();
        var self = ThisFile();

        var offenders = ProductProjects
            .Select(project => Path.Combine(root, project))
            .Where(Directory.Exists)
            .SelectMany(project => Directory.EnumerateFiles(project, "*", SearchOption.AllDirectories))
            .Where(file => TextExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .Where(file => !IsGeneratedOrVendored(file) && !string.Equals(Path.GetFullPath(file), self, StringComparison.OrdinalIgnoreCase))
            .SelectMany(file => File.ReadLines(file).Select((line, index) => (file, line, index)))
            .Where(x => DeploymentWords.IsMatch(WithoutFrozenNames(x.line)))
            .Select(x => $"{Path.GetRelativePath(root, x.file)}:{x.index + 1}: {x.line.Trim()}")
            .ToList();

        offenders.Should().BeEmpty("a deployment's words belong in its own host; the product says them through terminology and configuration");
    }

    [Test]
    public void The_generic_export_template_names_no_deployment()
    {
        var template = Path.Combine(RepositoryRoot(), "PlotboekGenerator.Host", "Templates", "PlotboekTemplate.docx");

        using var document = WordprocessingDocument.Open(template, false);
        var texts = new[] { document.MainDocumentPart!.Document!.Body!.InnerText }
            .Concat(document.MainDocumentPart.FooterParts.Select(footer => footer.Footer!.InnerText))
            .Concat(document.MainDocumentPart.HeaderParts.Select(header => header.Header!.InnerText));

        texts.Should().NotContain(text => DeploymentWords.IsMatch(text), "the template ships with the product");
        document.PackageProperties.Creator.Should().Be("Plotboek Generator", "the template carries no person's name");
    }

    /// <summary>The words of the first deployment that <paramref name="text"/> contains.</summary>
    /// <param name="text">Any text, such as a rendered page.</param>
    /// <returns>The words found; empty when there are none.</returns>
    internal static IReadOnlyList<string> DeploymentWordsIn(string text)
        => [.. DeploymentWords.Matches(text ?? string.Empty).Select(match => match.Value)];

    private static string WithoutFrozenNames(string line)
        => FrozenNames.Aggregate(line, (current, frozen) => current.Replace(frozen, string.Empty, StringComparison.Ordinal));

    private static bool IsGeneratedOrVendored(string file)
    {
        var parts = file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Contains("bin") || parts.Contains("obj")
            || file.Replace('\\', '/').Contains("/wwwroot/lib/", StringComparison.Ordinal);
    }

    /// <summary>The folder holding the product's projects, found from where the specs run.</summary>
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "PlotboekGenerator.core")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The product's projects were not found above the test directory.");
    }

    private static string ThisFile([CallerFilePath] string path = "") => Path.GetFullPath(path);
}
