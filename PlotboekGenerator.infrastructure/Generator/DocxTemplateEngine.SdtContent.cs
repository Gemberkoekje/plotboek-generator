using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace PlotboekGenerator.infrastructure.Generator;

public static partial class DocxTemplateEngine
{
    private static void SetSdtText(SdtElement sdt, string value)
    {
        switch (sdt)
        {
            case SdtCell sdtCell:
                SetTextInSdtCell(sdtCell, value);
                break;
            case SdtRow sdtRow:
                SetTextInSdtRow(sdtRow, value);
                break;
            case SdtRun sdtRun:
                SetTextInSdtRun(sdtRun, value);
                break;
            case SdtBlock sdtBlock:
                SetTextInSdtBlock(sdtBlock, value);
                break;
            default:
                SetTextInGenericSdt(sdt, value);
                break;
        }

        var pr = sdt.SdtProperties ??= new SdtProperties();
        pr.RemoveAllChildren<ShowingPlaceholder>();
    }

    private static void SetTextInSdtCell(SdtCell sdtCell, string value)
    {
        var tc = sdtCell.SdtContentCell?.GetFirstChild<TableCell>();
        if (tc == null) return;

        var run = tc.FirstOrDefault(e => e is Paragraph)?.FirstOrDefault(e => e is Run) as Run;
        if (run == null) return;

        ReplaceRunText(run, value);
    }

    private static void SetTextInSdtRow(SdtRow sdtRow, string value)
    {
        var rowContent = sdtRow.SdtContentRow;
        if (rowContent == null) return;

        var run = rowContent.FirstOrDefault(e => e is Paragraph)?.FirstOrDefault(e => e is Run);
        if (run == null) return;

        ReplaceRunText(run, value);
    }

    private static void SetTextInSdtRun(SdtRun sdtRun, string value)
    {
        var runContent = sdtRun.SdtContentRun;
        if (runContent == null) return;

        var run = runContent.GetFirstChild<Run>();
        if (run != null)
        {
            ReplaceRunText(run, value);
        }
        else
        {
            // Create a new Run and use ReplaceRunText to handle line breaks properly
            var newRun = new Run();
            ReplaceRunText(newRun, value);
            runContent.RemoveAllChildren<Text>();
            runContent.AppendChild(newRun);
        }
    }

    private static void SetTextInSdtBlock(SdtBlock sdtBlock, string value)
    {
        var blockContent = sdtBlock.SdtContentBlock;
        if (blockContent == null) return;

        var run = blockContent.FirstOrDefault(e => e is Paragraph)?.FirstOrDefault(e => e is Run);
        if (run == null) return;

        ReplaceRunText(run, value);
    }

    private static void SetTextInGenericSdt(SdtElement sdt, string value)
    {
        var content = sdt.ChildElements.FirstOrDefault(e =>
            e is SdtContentBlock or SdtContentRun or SdtContentCell or SdtContentRow);
        if (content == null) return;

        var run = content.FirstOrDefault(e => e is Paragraph)?.FirstOrDefault(e => e is Run);
        if (run == null) return;

        ReplaceRunText(run, value);
    }

    /// <summary>
    /// Writes <paramref name="value"/> into <paramref name="run"/>, turning every <c>\n</c> into a
    /// <see cref="Break"/>. Consecutive newlines therefore yield consecutive breaks, i.e. a blank line —
    /// which is what the multi-block "Kleding &amp; Schmink" cell relies on.
    /// </summary>
    /// <param name="run">The run whose content is replaced.</param>
    /// <param name="value">The text to write; may contain newlines.</param>
    internal static void ReplaceRunText(OpenXmlElement run, string value)
    {
        run.RemoveAllChildren<Text>();
        run.RemoveAllChildren<Break>();

        if (string.IsNullOrEmpty(value))
        {
            run.AppendChild(new Text(string.Empty) { Space = SpaceProcessingModeValues.Preserve });
            return;
        }

        // Split by newlines and insert Break elements between text segments
        var lines = value.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                // Insert a line break before subsequent lines
                run.AppendChild(new Break());
            }
            run.AppendChild(new Text(lines[i]) { Space = SpaceProcessingModeValues.Preserve });
        }
    }

    /// <summary>
    /// Unwraps an SDT element by replacing it with its content, preserving styles.
    /// </summary>
    private static void UnwrapSdt(SdtElement sdt)
    {
        var parent = sdt.Parent;
        if (parent == null) return;

        var contentContainer = GetContentContainer(sdt);
        if (contentContainer == null) return;

        UnwrapContentToParent(contentContainer, parent, sdt);
        sdt.Remove();
    }

    private static void UnwrapContentToParent(OpenXmlElement contentContainer, OpenXmlElement parent, OpenXmlElement insertBefore)
    {
        var children = contentContainer.ChildElements.ToList();
        foreach (var child in children)
        {
            var clone = child.CloneNode(true);
            parent.InsertBefore(clone, insertBefore);
        }
    }

    private static OpenXmlElement GetContentContainer(SdtElement sdt) =>
        sdt switch
        {
            SdtBlock b => b.SdtContentBlock,
            SdtRun r => r.SdtContentRun,
            SdtCell c => c.SdtContentCell,
            SdtRow rw => rw.SdtContentRow,
            _ => sdt.ChildElements.FirstOrDefault(e =>
                    e is SdtContentBlock or SdtContentRun or SdtContentCell or SdtContentRow)
        };

    /// <summary>
    /// Recursively unwraps all remaining SDT elements in the document, converting them to plain text.
    /// </summary>
    private static void UnwrapAllSdtElements(OpenXmlElement root)
    {
        // Keep unwrapping until no SDT elements remain (nested SDTs require multiple passes)
        bool foundSdt;
        do
        {
            foundSdt = false;
            var sdtElements = root.Descendants<SdtElement>().ToList();
            foreach (var sdt in sdtElements)
            {
                UnwrapSdt(sdt);
                foundSdt = true;
            }
        }
        while (foundSdt);
    }
}
