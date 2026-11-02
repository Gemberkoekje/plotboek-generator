namespace PlotboekGenerator.core.Enums;

/// <summary>
/// The lifecycle of a single feedback point on a <see cref="Models.PlotboekPagina"/>.
/// Only <see cref="Open"/> blocks the pagina from being marked "volledig klaar".
/// </summary>
public enum FeedbackStatus
{
    /// <summary>No status; the default for an unset value.</summary>
    None = 0,

    /// <summary>The HSL raised the point and the SL has not responded yet. Blocks "volledig klaar".</summary>
    Open = 1,

    /// <summary>The SL responded. Does not block; the HSL still has to sign it off.</summary>
    Beantwoord = 2,

    /// <summary>The HSL signed the point off. Does not block and folds away out of sight.</summary>
    Afgehandeld = 3,
}
