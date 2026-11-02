namespace PlotboekGenerator.core.Events;

public sealed record PlotboekGoogleDriveFolderUrlAangepast : AuditTrailEvent
{
    required public string GoogleDriveFolderUrl { get; init; }

    public override string What => "<strong>Google Drive folder URL aangepast</strong>";
}
