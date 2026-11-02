using System.Globalization;
using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekDagenAangepast : AuditTrailEvent
{
    required public Dag StartDag { get; init; }

    required public Dag EindDag { get; init; }

    public override string What => $"<strong>{CoreTekst.DagenAangepast}</strong> {string.Format(CultureInfo.CurrentCulture, CoreTekst.VanTotEnMet, StartDag.Naam(), EindDag.Naam())}";
}
