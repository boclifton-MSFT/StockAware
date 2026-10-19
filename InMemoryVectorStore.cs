using Microsoft.Extensions.VectorData;

namespace StockAware;

/// <summary>
/// Demo-only placeholder for a vector store. Storage operations are intentionally not implemented.
/// </summary>
internal sealed class InMemoryVectorStore : VectorStore
{
    public override VectorStoreCollection<TKey, TRecord> GetCollection<TKey, TRecord>(
        string name, VectorStoreCollectionDefinition? definition = null)
        => throw new NotSupportedException("This demo mock does not store records.");

    public override VectorStoreCollection<object, Dictionary<string, object?>> GetDynamicCollection(
        string name, VectorStoreCollectionDefinition definition)
        => throw new NotSupportedException("This demo mock does not store records.");

    public override IAsyncEnumerable<string> ListCollectionNamesAsync(
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("This demo mock does not manage collections.");

    public override Task<bool> CollectionExistsAsync(
        string name, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("This demo mock does not manage collections.");

    public override Task EnsureCollectionDeletedAsync(
        string name, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("This demo mock does not manage collections.");

    public override object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        return serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;
    }
}
