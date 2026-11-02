using System.Collections.Immutable;

namespace PlotboekGenerator.core.Generator.Templates;

/// <summary>
/// A merged document's bytes plus the page-layout estimator's own, uncorrected view of where
/// every bookmark landed. The bookmark data is the baseline a Google Drive sync cycle needs to
/// persist so the next cycle can compute a page-number correction from the actual Drive render.
/// </summary>
public sealed record GeneratedDocument
{
#pragma warning disable QW0012 // Use immutable types for properties
    required public byte[] Bytes { get; init; }
#pragma warning restore QW0012 // Use immutable types for properties

    /// <summary>Every resolved bookmark's estimated page, keyed by bookmark name.</summary>
    required public ImmutableDictionary<string, int> BookmarkPages { get; init; }

    /// <summary>
    /// For each bookmark in <see cref="BookmarkPages"/> that a Drive-render correction can be
    /// computed for, the exact heading/entity text to search for in a rendered PDF.
    /// </summary>
    required public ImmutableDictionary<string, string> BookmarkHeadingText { get; init; }
}
