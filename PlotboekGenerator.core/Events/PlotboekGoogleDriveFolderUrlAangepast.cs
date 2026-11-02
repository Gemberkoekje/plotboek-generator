using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekGoogleDriveFolderUrlAangepast : AuditTrailEvent
{
    required public string GoogleDriveFolderUrl { get; init; }

    public override string What => $"<strong>{CoreTekst.GoogleDriveFolderUrlAangepast}</strong>";
}
