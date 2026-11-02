using System.ComponentModel.DataAnnotations;

namespace PlotboekGenerator.core.Enums;

/// <summary>
/// Use <see cref="PlotboekGenerator.core.Models.ReferenceData.EventConfig"/> loaded from the database instead.
/// This enum is retained only for deserialising historical domain events and as the identity component of <see cref="PlotboekGenerator.core.Models.PlotboekId"/>.
/// </summary>
[Obsolete("Use EventConfig loaded from the database. This enum exists only for backward-compatible event deserialisation and PlotboekId identity.")]
public enum LegacyEvent
{
    None = 0,
    [Display(Name = "Moots 1")]
    Moots1,
    [Display(Name = "Summoning")]
    Summoning,
    [Display(Name = "Moots 2")]
    Moots2,
    [Display(Name = "Evergreen")]
    Evergreen,
}
