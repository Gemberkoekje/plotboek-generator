using System.Globalization;
using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaInstructiePentagonAangepast : AuditTrailEvent
{
    required public string InstructiePentagon { get; init; }

    public override string What => $"<strong>{string.Format(CultureInfo.CurrentCulture, CoreTekst.InstructieTechniekAangepast, Terminologie.Huidig.InstructieTechniek)}</strong>";

    public override string Details => string.IsNullOrWhiteSpace(InstructiePentagon) ? null : InstructiePentagon;
}
