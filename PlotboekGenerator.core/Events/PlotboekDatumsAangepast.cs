using System.Globalization;
using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekDatumsAangepast : AuditTrailEvent
{
    required public DateOnly? StartDatum { get; init; }

    required public DateOnly? EindDatum { get; init; }

    public override string What => $"<strong>{CoreTekst.DatumsAangepast}</strong> {string.Format(CultureInfo.CurrentCulture, CoreTekst.VanTotEnMet, StartDatum, EindDatum)}";
}
