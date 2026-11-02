namespace PlotboekGenerator.infrastructure.Services;

/// <summary>What a demo reset did, as the reset endpoint reports it.</summary>
public sealed record DemoResetSummary
{
    /// <summary>
    /// The number of visitors' accounts that were removed; the example's dummy spelleiders, which are
    /// written again straight away, are not counted.
    /// </summary>
    required public int RemovedAccounts { get; init; }
}
