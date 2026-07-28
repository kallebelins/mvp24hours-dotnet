using System.Collections.Concurrent;
using CustomerAPI.Domain.Aggregates;

namespace CustomerAPI.Application.Projections;

/// <summary>
/// In-memory projection that maintains a denormalized read model per customer.
///
/// In this sample the projection is updated <em>inline</em> immediately after saving
/// the aggregate, which is the simplest teaching approach. A production implementation
/// would subscribe to <c>IEventStoreWithSubscription.SubscribeFromPositionAsync</c>
/// in a background hosted service to update the projection asynchronously.
/// </summary>
public class CustomerProjection
{
    private readonly ConcurrentDictionary<Guid, CustomerReadModel> _store = new();

    /// <summary>
    /// Synchronously updates the read model from the aggregate's current state.
    /// Call this after every successful <c>IEventStoreRepository.SaveAsync</c>.
    /// </summary>
    public void Apply(CustomerAggregate aggregate)
    {
        _store.AddOrUpdate(
            aggregate.Id,
            _ => BuildModel(aggregate),
            (_, _) => BuildModel(aggregate));
    }

    public CustomerReadModel? GetById(Guid id)
        => _store.TryGetValue(id, out CustomerReadModel? model) ? model : null;

    public IReadOnlyList<CustomerReadModel> GetAll()
        => [.. _store.Values];

    private static CustomerReadModel BuildModel(CustomerAggregate aggregate) => new()
    {
        Id = aggregate.Id,
        Name = aggregate.Name,
        Email = aggregate.Email,
        IsActive = aggregate.IsActive,
        Version = aggregate.Version,
        LastModified = DateTime.UtcNow
    };
}
