using PlotboekGenerator.Application.Ports;
using PlotboekGenerator.infrastructure.Generator;
using PlotboekGenerator.core.Generator.Templates;

namespace PlotboekGenerator.infrastructure.Services;

/// <summary>
/// Generates DOCX documents for plotboek exports, merged into the deployment's template.
/// </summary>
/// <param name="templateProvider">Supplies the deployment's Word template.</param>
public sealed class DocumentGenerationService(IDocumentTemplateProvider templateProvider) : IDocumentGenerator
{
    private readonly IDocumentTemplateProvider _templateProvider = templateProvider ?? throw new ArgumentNullException(nameof(templateProvider));

    public Task<Result<byte[]>> GeneratePlotboekDocumentAsync(PlotboekTemplateData exportData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exportData);

        try
        {
            using var template = _templateProvider.OpenTemplate();
            var document = DocxTemplateEngine.MergeModelWithCorrections(template, exportData, null).Bytes;
            return Task.FromResult<Result<byte[]>>(document);
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.WithMessages<byte[]>(ValidationMessage.Error(ex.Message)));
        }
    }

    public Task<Result<GeneratedDocument>> GeneratePlotboekDocumentWithCorrectionsAsync(
        PlotboekTemplateData exportData,
        IReadOnlyDictionary<string, int> corrections,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exportData);
        ArgumentNullException.ThrowIfNull(corrections);

        try
        {
            using var template = _templateProvider.OpenTemplate();
            var merged = DocxTemplateEngine.MergeModelWithCorrections(template, exportData, corrections);
            var document = new GeneratedDocument
            {
                Bytes = merged.Bytes,
                BookmarkPages = merged.BookmarkPages,
                BookmarkHeadingText = merged.BookmarkHeadingText,
            };
            return Task.FromResult<Result<GeneratedDocument>>(document);
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.WithMessages<GeneratedDocument>(ValidationMessage.Error(ex.Message)));
        }
    }
}
