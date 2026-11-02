using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekGoogleDriveFileUrlAangepast : AuditTrailEvent
{
    required public string GoogleDriveFileUrl { get; init; }

    public override string What => $"<strong>{CoreTekst.GoogleDriveBestandUrlAangepast}</strong>";
}
