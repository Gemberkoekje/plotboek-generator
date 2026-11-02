namespace PlotboekGenerator.core.Events;

public sealed record PlotboekGoogleDriveFileUrlAangepast : AuditTrailEvent
{
    required public string GoogleDriveFileUrl { get; init; }

    public override string What => "<strong>Google Drive bestand URL aangepast</strong>";
}
