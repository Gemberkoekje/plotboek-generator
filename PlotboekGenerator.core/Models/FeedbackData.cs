namespace PlotboekGenerator.core.Models;

/// <summary>
/// A single feedback point raised by an HSL on a <see cref="PlotboekPagina"/>, together with the
/// writing SL's response and its lifecycle status.
///
/// Deliberately NOT part of <see cref="PlotboekPagina.Data"/>: feedback never travels along on a
/// regular pagina save, so an ordinary save can never wipe it, and the HSL and SL can work on the
/// same plot at the same time without having to carry each other's edits.
/// </summary>
public sealed record FeedbackData
{
    /// <summary>Stable identity, minted once when the point is raised and used by every follow-up action.</summary>
    public FeedbackId Id { get; init; }

    /// <summary>The feedback itself, as plain text.</summary>
    public string Tekst { get; init; } = string.Empty;

    /// <summary>User id of the HSL who raised the point.</summary>
    public string GegevenDoor { get; init; } = string.Empty;

    /// <summary>When the point was raised (UTC).</summary>
    public DateTime GegevenOp { get; init; }

    /// <summary>The SL's response, empty until someone responds.</summary>
    public string Reactie { get; init; } = string.Empty;

    /// <summary>User id of whoever responded, empty until someone does.</summary>
    public string ReactieDoor { get; init; } = string.Empty;

    /// <summary>
    /// When the response was written (UTC). Stays <c>default</c> while there is no response —
    /// <see cref="Status"/> is the discriminator, not a nullable date.
    /// </summary>
    public DateTime ReactieOp { get; init; }

    /// <summary>Where the point sits in its lifecycle. Only <see cref="FeedbackStatus.Open"/> blocks completion.</summary>
    public FeedbackStatus Status { get; init; } = FeedbackStatus.None;

    /// <summary>A feedback point is meaningful only once it carries text.</summary>
    public bool HasValue => !string.IsNullOrWhiteSpace(Tekst);

    /// <summary>Whether the point has a response to show.</summary>
    public bool HeeftReactie => !string.IsNullOrWhiteSpace(Reactie);

    /// <summary>Creates a freshly raised, still-open feedback point.</summary>
    public static FeedbackData Create(FeedbackId id, string tekst, string gegevenDoor, DateTime gegevenOp)
    {
        return new FeedbackData
        {
            Id = id,
            Tekst = tekst ?? string.Empty,
            GegevenDoor = gegevenDoor ?? string.Empty,
            GegevenOp = gegevenOp,
            Status = FeedbackStatus.Open,
        };
    }
}
