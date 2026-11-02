namespace PlotboekGenerator.infrastructure;

/// <summary>
/// Factory for creating <see cref="IDocumentSession"/> instances.
/// </summary>
public interface IDocumentStore
{
    /// <summary>Creates a new session for querying and mutating documents.</summary>
    IDocumentSession OpenSession();
}
