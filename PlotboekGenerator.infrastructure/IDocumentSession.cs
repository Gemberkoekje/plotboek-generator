using System.Linq.Expressions;

namespace PlotboekGenerator.infrastructure;

/// <summary>
/// Represents a unit-of-work session for querying and mutating documents in the store.
/// </summary>
public interface IDocumentSession : IDisposable
{
    /// <summary>Returns all documents of type <typeparamref name="T"/>.</summary>
    Task<IReadOnlyList<T>> QueryAllAsync<T>(CancellationToken ct = default) where T : class;

    /// <summary>Returns all documents of type <typeparamref name="T"/> matching the <paramref name="filter"/>.</summary>
    Task<IReadOnlyList<T>> QueryAsync<T>(Expression<Func<T, bool>> filter, CancellationToken ct = default) where T : class;

    /// <summary>Returns the first matching document, or <c>null</c>.</summary>
    Task<T> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> filter, CancellationToken ct = default) where T : class;

    /// <summary>Returns exactly one matching document, or <c>null</c>. Throws if more than one match.</summary>
    Task<T> SingleOrDefaultAsync<T>(Expression<Func<T, bool>> filter, CancellationToken ct = default) where T : class;

    /// <summary>Loads a document by its string identifier.</summary>
    Task<T> LoadAsync<T>(string id, CancellationToken ct = default) where T : class;

    /// <summary>Loads a document by its <see cref="Guid"/> identifier.</summary>
    Task<T> LoadAsync<T>(Guid id, CancellationToken ct = default) where T : class;

    /// <summary>Stages a document for persistence, inserting or overwriting it.</summary>
    void Store<T>(T entity) where T : class;

    /// <summary>
    /// Stages a document for insertion only. Unlike <see cref="Store{T}"/> this never overwrites:
    /// <see cref="SaveChangesAsync"/> throws <see cref="DocumentAlreadyExistsException"/> when a
    /// document with the same identity is already stored. Use it where "create" must not silently
    /// clobber a record another request created in the meantime.
    /// </summary>
    void Insert<T>(T entity) where T : class;

    /// <summary>Stages deletion of a document by its string identifier.</summary>
    void Delete<T>(string id) where T : class;

    /// <summary>Stages deletion of a document entity.</summary>
    void Delete<T>(T entity) where T : class;

    /// <summary>Flushes all pending changes to the underlying store.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
