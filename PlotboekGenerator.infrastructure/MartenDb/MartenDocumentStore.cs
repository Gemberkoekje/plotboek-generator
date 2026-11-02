namespace PlotboekGenerator.infrastructure.MartenDb;

/// <summary>
/// <see cref="IDocumentStore"/> implementation backed by Marten's <see cref="Marten.IDocumentStore"/>.
/// </summary>
public sealed class MartenDocumentStore(Marten.IDocumentStore inner) : IDocumentStore
{
    public IDocumentSession OpenSession()
        => new MartenDocumentSession(inner);
}
