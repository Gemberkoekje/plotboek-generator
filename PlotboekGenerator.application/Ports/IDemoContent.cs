namespace PlotboekGenerator.Application.Ports;

/// <summary>
/// The example a demo shows (see <c>Demo:Enabled</c>): written into a database without plotboeken
/// when the demo starts, and again after every reset. Only a deployment that can run as a demo
/// registers one.
/// </summary>
/// <remarks>
/// It refers to the deployment's reference data by id (events, start times, block lengths,
/// encounter types, quest locations) and to its domains by shorthand, so it is written after that
/// data is seeded.
/// </remarks>
public interface IDemoContent
{
    /// <summary>The spelleiders the example's plots belong to, written as dummy users.</summary>
    IReadOnlyList<DemoSpelleider> Spelleiders { get; }

    /// <summary>The example plotboeken, each written for the current year.</summary>
    IReadOnlyList<DemoPlotboek> Plotboeken { get; }
}
