using System.ComponentModel.DataAnnotations;

namespace PlotboekGenerator.core.Enums;

/// <summary>
/// Use <see cref="PlotboekGenerator.core.Models.ReferenceData.AantalBlokkenConfig"/> loaded from the database instead.
/// This enum is retained only for deserialising historical domain events.
/// </summary>
[Obsolete("Use AantalBlokkenConfig loaded from the database. This enum exists only for backward-compatible event deserialisation.")]
public enum AantalBlokken
{
    [Display(Name = "-")]
    None = 0,

    [Display(Name = "1 blok (max 1 uur)")]
    EenBlok,

    [Display(Name = "2 blokken (+/- 2.5 uur)")]
    TweeBlokken,

    [Display(Name = "3 blokken (+/- 4.5 uur)")]
    DrieBlokken,
}
