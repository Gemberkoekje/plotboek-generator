namespace PlotboekGenerator.core.Models;

public partial class Plotboek
{
    public string GoogleDriveFolderUrl { get; private set; }

    public Result<Plotboek> Aanpassen(GoogleDriveFolderUrlData data, PlotboekUser updatedBy)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (GoogleDriveFolderUrl != data.GoogleDriveFolderUrl)
        {
            return ApplyEvent(new PlotboekGoogleDriveFolderUrlAangepast
            {
                GoogleDriveFolderUrl = data.GoogleDriveFolderUrl,
                Who = updatedBy.Id,
            });
        }

        return Result.For(this);
    }

    internal void When(PlotboekGoogleDriveFolderUrlAangepast @event)
    {
        GoogleDriveFolderUrl = @event.GoogleDriveFolderUrl;
    }

    public sealed class GoogleDriveFolderUrlData
    {
        public string GoogleDriveFolderUrl { get; init; }
    }
}
