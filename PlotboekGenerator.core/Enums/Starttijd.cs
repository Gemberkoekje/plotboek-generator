using System.ComponentModel.DataAnnotations;

namespace PlotboekGenerator.core.Enums;

/// <summary>
/// Use <see cref="PlotboekGenerator.core.Models.ReferenceData.StarttijdConfig"/> loaded from the database instead.
/// This enum is retained only for deserialising historical domain events.
/// </summary>
[Obsolete("Use StarttijdConfig loaded from the database. This enum exists only for backward-compatible event deserialisation.")]
public enum Starttijd
{
    [Display(Name = "-")]
    None = 0,

    [Display(Name = "10:30")]
    Tijd1030,

    [Display(Name = "12:30")]
    Tijd1230,

    [Display(Name = "14:30")]
    Tijd1430,

    [Display(Name = "16:30")]
    Tijd1630,

    [Display(Name = "19:30: Introquest")]
    Tijd1930Introquest,

    [Display(Name = "20:00")]
    Tijd2000,

    [Display(Name = "22:00")]
    Tijd2200,

    [Display(Name = "00:00")]
    Tijd0000,
}
