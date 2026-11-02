using System.Net;

namespace PlotboekGenerator.core.Events;

/// <summary>
/// Represents the base class for all audit trail events in the system.
/// Captures who performed the action, when it occurred, and what was done.
/// </summary>
public abstract record AuditTrailEvent
{
    /// <summary>
    /// Gets or sets the unique identifier of the user or system that performed the action.
    /// </summary>
    required public string Who { get; init; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the event occurred.
    /// Defaults to the current UTC time.
    /// </summary>
    public DateTime When { get; init; } = Clock.UtcNow();

    /// <summary>
    /// Gets a description of the action that was performed.
    /// </summary>
    public abstract string What { get; }

    /// <summary>
    /// Gets optional expandable details for the action (e.g. full HTML content or multi-line breakdown).
    /// Returns <c>null</c> when there are no additional details to show.
    /// </summary>
    public virtual string Details => null;

    /// <summary>
    /// Renders user-supplied <em>plain</em> text for the <see cref="Details"/> pane, which the audit
    /// modal writes with <c>Html.Raw</c>. Encodes the text so markup typed by a user can never become
    /// live HTML, and turns line breaks into <c>&lt;br /&gt;</c> so multi-line remarks stay readable.
    /// Rich-text fields that already hold sanitized Quill HTML must NOT go through this.
    /// </summary>
    /// <param name="text">The raw text as the user typed it.</param>
    /// <returns>Encoded HTML, or <c>null</c> when there is nothing to show.</returns>
    protected static string PlainTextDetails(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return PlainTextInline(text)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\n", "<br />", StringComparison.Ordinal);
    }

    /// <summary>
    /// Renders user-supplied <em>plain</em> text for inline use inside a <see cref="What"/> summary,
    /// which the audit modal also writes with <c>Html.Raw</c>. Same encoding contract as
    /// <see cref="PlainTextDetails"/>, but line breaks are left alone because <see cref="What"/> is a
    /// one-line summary. Every value interpolated into a <see cref="What"/> that a user can type
    /// freely must go through this; enum values and ids the domain generates itself need not.
    /// Rich-text fields that already hold sanitized Quill HTML must NOT go through this.
    /// </summary>
    /// <param name="text">The raw text as the user typed it.</param>
    /// <returns>Encoded HTML, or an empty string when there is nothing to show.</returns>
    protected static string PlainTextInline(string text)
        => string.IsNullOrEmpty(text) ? string.Empty : WebUtility.HtmlEncode(text);
}
