namespace PlotboekGenerator.infrastructure.MartenDb;

/// <summary>Implements <see cref="IEventStoreWiper"/> by emptying Marten's event tables.</summary>
public sealed class MartenEventStoreWiper(Marten.IDocumentStore store) : IEventStoreWiper
{
    private readonly Marten.IDocumentStore _store = store ?? throw new ArgumentNullException(nameof(store));

    /// <inheritdoc />
    public Task WipeAllAsync(CancellationToken cancellationToken = default)
        => _store.Advanced.Clean.DeleteAllEventDataAsync(cancellationToken);
}
