namespace PlotboekGenerator.core.Enums;

/// <summary>
/// How serious the reservation on a quest location category is. Drives the styling of the
/// contextual note under the questlocatie picker.
/// </summary>
public enum QuestLocatieErnst
{
    /// <summary>No value set.</summary>
    None = 0,

    /// <summary>No reservation at all; the location can always be used.</summary>
    Geen,

    /// <summary>Usable, but something needs checking first (rendered as an info alert).</summary>
    LetOp,

    /// <summary>Discouraged; only pick this when there is really no alternative (rendered as a warning alert).</summary>
    Afgeraden,
}
