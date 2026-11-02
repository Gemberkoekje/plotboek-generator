using System.IO;
using System.Reflection;
using PlotboekGenerator.Application.Ports;

namespace PlotboekGenerator.infrastructure.Generator;

/// <summary>
/// An <see cref="IDocumentTemplateProvider"/> over a .docx embedded in an assembly, typically the
/// host's own.
/// </summary>
/// <param name="assembly">The assembly that embeds the template.</param>
/// <param name="resourceName">The manifest resource name, e.g. <c>MyHost.Templates.Plotboek.docx</c>.</param>
public sealed class EmbeddedResourceTemplateProvider(Assembly assembly, string resourceName) : IDocumentTemplateProvider
{
    private readonly Assembly _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
    private readonly string _resourceName = resourceName ?? throw new ArgumentNullException(nameof(resourceName));

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">When the assembly does not embed the resource.</exception>
    public Stream OpenTemplate()
        => _assembly.GetManifestResourceStream(_resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {_resourceName} in {_assembly.GetName().Name}");
}
