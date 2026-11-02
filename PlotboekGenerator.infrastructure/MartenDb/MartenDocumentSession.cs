using System.Linq.Expressions;
using Marten;
using Npgsql;

namespace PlotboekGenerator.infrastructure.MartenDb;

/// <summary>
/// <see cref="IDocumentSession"/> implementation backed by a Marten <see cref="Marten.IDocumentSession"/>.
/// </summary>
internal sealed class MartenDocumentSession : IDocumentSession
{
    private readonly Marten.IDocumentSession inner;

    public MartenDocumentSession(Marten.IDocumentStore store)
    {
        inner = store.LightweightSession();
    }

    public async Task<IReadOnlyList<T>> QueryAllAsync<T>(CancellationToken ct = default) where T : class
        => await inner.Query<T>().ToListAsync(ct);

    public async Task<IReadOnlyList<T>> QueryAsync<T>(Expression<Func<T, bool>> filter, CancellationToken ct = default) where T : class
        => await inner.Query<T>().Where(filter).ToListAsync(ct);

    public Task<T> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> filter, CancellationToken ct = default) where T : class
        => inner.Query<T>().FirstOrDefaultAsync(filter, ct);

    public Task<T> SingleOrDefaultAsync<T>(Expression<Func<T, bool>> filter, CancellationToken ct = default) where T : class
        => inner.Query<T>().SingleOrDefaultAsync(filter, ct);

    public Task<T> LoadAsync<T>(string id, CancellationToken ct = default) where T : class
        => inner.LoadAsync<T>(id, ct);

    public Task<T> LoadAsync<T>(Guid id, CancellationToken ct = default) where T : class
        => inner.LoadAsync<T>(id, ct);

    public void Store<T>(T entity) where T : class
    {
        inner.Store(entity);
    }

    public void Insert<T>(T entity) where T : class
    {
        inner.Insert(entity);
    }

    public void Delete<T>(string id) where T : class
        => inner.Delete<T>(id);

    public void Delete<T>(T entity) where T : class
        => inner.Delete(entity);

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await inner.SaveChangesAsync(ct);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            // An Insert lost the race against a concurrent one. Translate it here so callers can
            // recognise a duplicate without referencing Npgsql.
            throw new DocumentAlreadyExistsException("A document with the same identity already exists.", ex);
        }
    }

    public void Dispose() => inner.Dispose();
}
