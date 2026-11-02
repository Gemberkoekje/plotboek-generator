namespace PlotboekGenerator.core.Models;

public partial class Plotboek
{
    public string GoogleDriveFileUrl { get; private set; }

    public Result<Plotboek> Aanpassen(GoogleDriveFileUrlData data, PlotboekUser updatedBy)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (GoogleDriveFileUrl != data.GoogleDriveFileUrl)
        {
            return ApplyEvent(new PlotboekGoogleDriveFileUrlAangepast
            {
                GoogleDriveFileUrl = data.GoogleDriveFileUrl,
                Who = updatedBy.Id,
            });
        }

        return Result.For(this);
    }

    internal void When(PlotboekGoogleDriveFileUrlAangepast @event)
    {
        GoogleDriveFileUrl = @event.GoogleDriveFileUrl;
    }

    public sealed class GoogleDriveFileUrlData
    {
        public string GoogleDriveFileUrl { get; init; }
    }
}
