namespace PlotboekGenerator.core.Models;

/// <summary>
/// Stores the front page image for a Plotboek.
/// Stored as a separate Marten document to avoid bloating the event store with binary data.
/// </summary>
public sealed record PlotboekVoorblad
{
    /// <summary>
    /// The PlotboekId as a string, used as the Marten document identity.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// The raw image bytes of the front page image.
    /// </summary>
    /// <remarks>
    /// Kept as <c>byte[]</c> because System.Text.Json (used by Marten)
    /// serialises it as compact base64 — <see cref="System.Collections.Immutable.ImmutableArray{T}"/>
    /// would produce a JSON number array instead.
    /// </remarks>
#pragma warning disable QW0012 // Use immutable types for properties
    public byte[] Afbeelding { get; init; } = [];
#pragma warning restore QW0012 // Use immutable types for properties

    /// <summary>
    /// The MIME content type of the image (e.g. "image/png", "image/jpeg").
    /// </summary>
    public string ContentType { get; init; } = string.Empty;
}
