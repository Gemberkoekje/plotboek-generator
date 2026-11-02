using System.Text.RegularExpressions;

namespace PlotboekGenerator.core;

public static partial class TextNormalization
{
    public static string Normalize(string input)
    {
        // Normalize line endings to LF and trim trailing whitespace so comparisons are stable.
        // Also remove trailing spaces at the end of each line to avoid false-positive
        // changes introduced by editors that only differ in per-line trailing whitespace.
        var s = input ?? string.Empty;

        // Normalize all CRLF and CR to LF
        s = s.Replace("\r\n", "\n").Replace("\r", "\n");

        // Replace common HTML non-breaking space entity with normal space so it is
        // treated as whitespace by Trim operations below.
        s = s.Replace("&nbsp;", " ");

        // Remove zero-width characters that sometimes appear when copying from editors
        s = s.Replace("\u200B", string.Empty)
             .Replace("\u200C", string.Empty)
             .Replace("\u200D", string.Empty)
             .Replace("\uFEFF", string.Empty);

        if (s.Length == 0)
        {
            return string.Empty;
        }

        // Trim end of each line to remove trailing spaces/tabs
        var lines = s.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            lines[i] = lines[i].TrimEnd();
        }

        // Rejoin and trim any trailing blank lines/whitespace
        var result = string.Join("\n", lines).TrimEnd();

        // Collapse multiple final blank lines into none
        while (result.EndsWith("\n\n", StringComparison.Ordinal))
        {
            result = result[..^1];
        }

        // Remove quill editor tooltip or clipboard containers if accidentally included
        try
        {
            var emptyQuillDocumentRegex = EmptyQuillDocumentRegex();
            result = emptyQuillDocumentRegex.Replace(result, string.Empty);

            // Remove quill tooltip/clipboard containers if included
            var tooltipRegex = TooltipRegex();
            result = tooltipRegex.Replace(result, string.Empty);

            // Also remove any trailing empty block elements that rich-text editors often append
            // e.g. <p><br></p>, <p></p>, <div><br></div> and similar.
            var emptyBlockRegex = EmptyBlockRegex();
            result = emptyBlockRegex.Replace(result, string.Empty);

            // Also strip repeated empty block elements that appear just before the closing
            // of a Quill editor container, e.g. <div class="ql-editor">...<p><br></p></div>
            var qlTrailingEmptyRegex = QlTrailingEmptyRegex();
            result = qlTrailingEmptyRegex.Replace(result, "$1$2");

            // Additionally remove sequences of empty blocks immediately before a closing
            // </div> that might not be captured by the full ql-editor wrapper regex.
            var emptyBeforeCloseRegex = EmptyBeforeCloseRegex();
            result = emptyBeforeCloseRegex.Replace(result, string.Empty);
        }
        catch
        {
            // If regex fails for any reason, fall back to the raw result
        }

        return result.Trim();
    }

    /// <summary>
    /// Write a debug log containing a readable and byte-level representation of
    /// the current and incoming values. Useful to diagnose false-positive
    /// change detection during tests or running application.
    /// </summary>
    public static void DebugLogComparison(string fieldName, string current, string incoming)
    {
        try
        {
            var cur = Normalize(current);
            var inc = Normalize(incoming);

            System.Diagnostics.Debug.WriteLine($"[TextNormalization] Field: {fieldName}");
            System.Diagnostics.Debug.WriteLine($"[TextNormalization] Current (len={cur.Length}): '{cur}'");
            System.Diagnostics.Debug.WriteLine($"[TextNormalization] Incoming (len={inc.Length}): '{inc}'");

            var curBytes = System.Text.Encoding.UTF8.GetBytes(cur ?? string.Empty);
            var incBytes = System.Text.Encoding.UTF8.GetBytes(inc ?? string.Empty);
            System.Diagnostics.Debug.WriteLine($"[TextNormalization] Current bytes: {BitConverter.ToString(curBytes)}");
            System.Diagnostics.Debug.WriteLine($"[TextNormalization] Incoming bytes: {BitConverter.ToString(incBytes)}");
        }
        catch (Exception ex)
        {
            // Avoid throwing from debug helper
            System.Diagnostics.Debug.WriteLine($"[TextNormalization] DebugLogComparison failed: {ex}");
        }
    }

    [GeneratedRegex("(?is)^\\s*<div\\s+class=[\"']ql-editor\\b[^>]*>\\s*(?:<(?:p|div)[^>]*>\\s*(?:<br\\s*/?>|&nbsp;|\\s)*\\s*</(?:p|div)>\\s*)*</div>(?:\\s*<div\\s+class=[\"']ql-(?:tooltip|clipboard)\\b[^>]*>.*?</div>\\s*)*$", RegexOptions.None, "nl-NL")]
    private static partial Regex EmptyQuillDocumentRegex();

    [GeneratedRegexAttribute("(?is)<div\\s+class=[\\'\\\"]ql-(?:tooltip|clipboard)\\b[^>]*>.*?</div>\\s*$", RegexOptions.None, "nl-NL")]
    private static partial System.Text.RegularExpressions.Regex TooltipRegex();

    [GeneratedRegexAttribute("(?is)(?:\\s*(?:<(?:p|div)[^>]*>\\s*(?:<br\\s*/?>|&nbsp;|\\s)*\\s*</(?:p|div)>\\s*)+)$", RegexOptions.None, "nl-NL")]
    private static partial System.Text.RegularExpressions.Regex EmptyBlockRegex();

    [GeneratedRegex("(?is)(<div\\s+class=[\"']ql-editor[^>]*>.*?)(?:\\s*(?:<(?:p|div)[^>]*>\\s*(?:<br\\s*/?>|&nbsp;|\\s)*\\s*</(?:p|div)>\\s*)+)(\\s*</div>)", RegexOptions.None, "nl-NL")]
    private static partial Regex QlTrailingEmptyRegex();

    [GeneratedRegex("(?is)(?:\\s*(?:<(?:p|div)[^>]*>\\s*(?:<br\\s*/?>|&nbsp;|\\s)*\\s*</(?:p|div)>\\s*)+)(?=\\s*</div>)", RegexOptions.None, "nl-NL")]
    private static partial Regex EmptyBeforeCloseRegex();
}
