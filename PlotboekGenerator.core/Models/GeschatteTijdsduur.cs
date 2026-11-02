using System.Globalization;
using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Models;

/// <summary>
/// Represents an estimated duration with optional minimum and maximum values.
/// If only minimum is provided, it's treated as an exact duration.
/// Automatically converts minutes to hours and minutes (e.g., 90 minutes → 1 uur en 30 minuten).
/// </summary>
public readonly record struct GeschatteTijdsduur
{
    public int? MinimumMinuten { get; init; }
    public int? MaximumMinuten { get; init; }

    public bool HasValue => MinimumMinuten.HasValue;
    public bool IsRange => MinimumMinuten.HasValue && MaximumMinuten.HasValue && MinimumMinuten != MaximumMinuten;

    public static GeschatteTijdsduur Empty => new ();

    public static GeschatteTijdsduur Create(int? minimumMinuten, int? maximumMinuten = null)
    {
        return new GeschatteTijdsduur
        {
            MinimumMinuten = minimumMinuten,
            MaximumMinuten = maximumMinuten,
        };
    }

    public override string ToString()
    {
        if (!HasValue)
            return string.Empty;

        if (IsRange)
        {
            return $"{FormatDuration(MinimumMinuten!.Value)} - {FormatDuration(MaximumMinuten!.Value)}";
        }

        var value = MaximumMinuten ?? MinimumMinuten;
        return FormatDuration(value!.Value);
    }

    private static string FormatDuration(int totalMinutes)
    {
        if (totalMinutes <= 0)
            return string.Empty;

        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;

        if (hours == 0)
        {
            return Minuten(minutes);
        }

        if (minutes == 0)
        {
            return Uren(hours);
        }

        return string.Format(CultureInfo.CurrentCulture, CoreTekst.TijdsduurUurEnMinuten, Uren(hours), Minuten(minutes));
    }

    private static string Uren(int hours)
        => string.Format(CultureInfo.CurrentCulture, hours == 1 ? CoreTekst.TijdsduurUurOne : CoreTekst.TijdsduurUurMany, hours);

    private static string Minuten(int minutes)
        => string.Format(CultureInfo.CurrentCulture, minutes == 1 ? CoreTekst.TijdsduurMinutenOne : CoreTekst.TijdsduurMinutenMany, minutes);
}
