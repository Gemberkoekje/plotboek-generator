namespace PlotboekGenerator.core.Models;

public partial class Plotboek
{
    public ExportData GetData()
    {
        return new ExportData
        {
            PlotboekId = Id,
            EventVersion = Version,
            Jaar = Jaar,
            EventId = EventId,
            StartDag = StartDag,
            EindDag = EindDag,
            StartDatum = StartDatum,
            EindDatum = EindDatum,
            Titel = Titel,
            SamenvattingAlgemeenPlot = SamenvattingAlgemeenPlot,
            GoogleDriveFolderUrl = GoogleDriveFolderUrl,
            GoogleDriveFileUrl = GoogleDriveFileUrl,
            NpcsDagTotalen = NpcsDagTotalen,
            Bijlagen = Bijlagen,
            IsVolledigKlaar = IsVolledigKlaar,
        };
    }

    public sealed class ExportData
    {
        public string Id { get => PlotboekId.ToString(); }

        public PlotboekId PlotboekId { get; init; }

        public long EventVersion { get; init; }

        public string DisplayName
        {
            get
            {
                return $"{(!string.IsNullOrWhiteSpace(Titel) ? $"{Titel} (" : string.Empty)}{EventId} {Jaar}{(!string.IsNullOrWhiteSpace(Titel) ? ")" : string.Empty)}";
            }
        }

        public Year Jaar { get; init; }

        public string EventId { get; init; }

        public Dag StartDag { get; init; }

        public Dag EindDag { get; init; }

        public DateOnly? StartDatum { get; init; }

        public DateOnly? EindDatum { get; init; }

        public string Titel { get; init; }

        public string SamenvattingAlgemeenPlot { get; init; }

        public string GoogleDriveFolderUrl { get; init; }

        public string GoogleDriveFileUrl { get; init; }

        public IReadOnlyDictionary<Dag, int> NpcsDagTotalen { get; init; } = new Dictionary<Dag, int>();

        public IReadOnlyList<BijlageData> Bijlagen { get; init; } = [];

        public bool IsVolledigKlaar { get; init; }
    }
}
