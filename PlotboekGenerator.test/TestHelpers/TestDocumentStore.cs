#nullable disable

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using PlotboekGenerator.infrastructure;

namespace PlotboekGenerator.test.TestHelpers;

/// <summary>
/// In-memory implementation of <see cref="IDocumentStore"/> for integration tests.
/// Provides a real backing store so tests can seed data, execute queries,
/// and assert on persisted documents without requiring PostgreSQL.
/// </summary>
internal sealed class TestDocumentStore : IDocumentStore
{
    private readonly ConcurrentDictionary<(Type, string), object> _documents = new ();
    private readonly ConcurrentDictionary<Type, int> _writeCounts = new ();

    /// <summary>Creates a new <see cref="IDocumentSession"/> backed by the shared in-memory store.</summary>
    public IDocumentSession OpenSession() => new InMemoryDocumentSession(this);

    /// <summary>Removes all documents. Used to isolate integration tests.</summary>
    public void Clear()
    {
        _documents.Clear();
        _writeCounts.Clear();
    }

    /// <summary>
    /// How often a document of this type was written, stored or inserted. Lets a spec prove that a second
    /// run of an idempotent seed leaves rows alone instead of rewriting them with identical content, which
    /// comparing the stored documents cannot show.
    /// </summary>
    internal int WriteCount<T>() where T : class
        => _writeCounts.TryGetValue(typeof(T), out var count) ? count : 0;

    /// <summary>Seeds one or more documents into the in-memory store.</summary>
    public void Seed<T>(params T[] documents) where T : class
    {
        foreach (var doc in documents)
        {
            StoreEntity(doc);
        }
    }

    /// <summary>Loads a single document by string ID for assertion purposes.</summary>
    public T Load<T>(string id) where T : class
    {
        return _documents.TryGetValue((typeof(T), id), out var doc) ? (T)doc : null;
    }

    /// <summary>Returns all documents of a given type.</summary>
    public IReadOnlyList<T> GetAll<T>() where T : class
    {
        return [.. _documents
            .Where(kv => kv.Key.Item1 == typeof(T))
            .Select(kv => (T)kv.Value)];
    }

    internal void StoreEntity(object entity)
    {
        var type = entity.GetType();
        var id = GetId(entity, type);
        _documents[(type, id)] = entity;
        CountWrite(type);
    }

    /// <summary>
    /// Stores an entity only when its identity is still free, mirroring the insert-only semantics the
    /// Marten session gets from the primary key.
    /// </summary>
    /// <returns><c>true</c> when it was inserted, <c>false</c> when the identity was taken.</returns>
    internal bool TryInsertEntity(object entity)
    {
        var type = entity.GetType();
        var id = GetId(entity, type);
        if (!_documents.TryAdd((type, id), entity))
        {
            return false;
        }

        CountWrite(type);
        return true;
    }

    private void CountWrite(Type type) => _writeCounts.AddOrUpdate(type, 1, (_, count) => count + 1);

    internal void DeleteEntity(Type type, string id)
    {
        _documents.TryRemove((type, id), out _);
    }

    internal void DeleteEntity(object entity)
    {
        var type = entity.GetType();
        var id = GetId(entity, type);
        _documents.TryRemove((type, id), out _);
    }

    private static string GetId(object entity, Type type)
    {
        var idProp = type.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance) ?? throw new InvalidOperationException($"Type {type.Name} does not have an Id property");
        var value = idProp.GetValue(entity);
        return value?.ToString() ?? throw new InvalidOperationException($"Id is null on {type.Name}");
    }

    /// <summary>
    /// In-memory <see cref="IDocumentSession"/> that delegates to the shared <see cref="TestDocumentStore"/>.
    /// Expression filters are compiled and evaluated against the in-memory collection.
    /// </summary>
    private sealed class InMemoryDocumentSession(TestDocumentStore store) : IDocumentSession
    {
        /// <summary>Insert-only documents, held back until SaveChanges like Marten does.</summary>
        private readonly List<object> _pendingInserts = [];

        public Task<IReadOnlyList<T>> QueryAllAsync<T>(CancellationToken ct = default) where T : class
            => Task.FromResult(store.GetAll<T>());

        public Task<IReadOnlyList<T>> QueryAsync<T>(Expression<Func<T, bool>> filter, CancellationToken ct = default) where T : class
        {
            var compiled = filter.Compile();
            IReadOnlyList<T> result = [.. store.GetAll<T>().Where(compiled)];
            return Task.FromResult(result);
        }

        public Task<T> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> filter, CancellationToken ct = default) where T : class
            => Task.FromResult(store.GetAll<T>().FirstOrDefault(filter.Compile()));

        public Task<T> SingleOrDefaultAsync<T>(Expression<Func<T, bool>> filter, CancellationToken ct = default) where T : class
            => Task.FromResult(store.GetAll<T>().SingleOrDefault(filter.Compile()));

        public Task<T> LoadAsync<T>(string id, CancellationToken ct = default) where T : class
            => Task.FromResult(store.Load<T>(id));

        public Task<T> LoadAsync<T>(Guid id, CancellationToken ct = default) where T : class
            => Task.FromResult(store.Load<T>(id.ToString()));

        public void Store<T>(T entity) where T : class
            => store.StoreEntity(entity);

        public void Insert<T>(T entity) where T : class
            => _pendingInserts.Add(entity);

        public void Delete<T>(string id) where T : class
            => store.DeleteEntity(typeof(T), id);

        public void Delete<T>(T entity) where T : class
            => store.DeleteEntity(entity);

        public Task SaveChangesAsync(CancellationToken ct = default)
        {
            // Marten only discovers an insert collision when it hits the primary key at commit time,
            // so the fake has to fail at the same moment for a caller's try/catch to line up.
            var pending = _pendingInserts.ToList();
            _pendingInserts.Clear();
            foreach (var entity in pending)
            {
                if (!store.TryInsertEntity(entity))
                {
                    throw new DocumentAlreadyExistsException("A document with the same identity already exists.");
                }
            }

            return Task.CompletedTask;
        }

        public void Dispose() { }
    }
}

