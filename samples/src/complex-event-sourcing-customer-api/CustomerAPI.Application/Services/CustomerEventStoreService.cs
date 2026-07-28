using CustomerAPI.Application.Projections;
using CustomerAPI.Domain.Aggregates;
using Mvp24Hours.Infrastructure.Cqrs.EventSourcing;

namespace CustomerAPI.Application.Services;

/// <summary>
/// Application service that wraps <see cref="IEventStoreRepository{CustomerAggregate}"/>
/// and keeps the <see cref="CustomerProjection"/> read model in sync.
/// </summary>
public class CustomerEventStoreService(
    IEventStoreRepository<CustomerAggregate> repository,
    CustomerProjection projection)
{
    public async Task<Guid> CreateAsync(string name, string email, CancellationToken cancellationToken = default)
    {
        CustomerAggregate aggregate = CustomerAggregate.Create(name, email);
        await repository.SaveAsync(aggregate, cancellationToken);
        projection.Apply(aggregate);
        return aggregate.Id;
    }

    public async Task RenameAsync(Guid id, string newName, CancellationToken cancellationToken = default)
    {
        CustomerAggregate aggregate = await LoadOrThrowAsync(id, cancellationToken);
        aggregate.Rename(newName);
        await repository.SaveAsync(aggregate, cancellationToken);
        projection.Apply(aggregate);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        CustomerAggregate aggregate = await LoadOrThrowAsync(id, cancellationToken);
        aggregate.Deactivate();
        await repository.SaveAsync(aggregate, cancellationToken);
        projection.Apply(aggregate);
    }

    /// <summary>
    /// Returns the projection read model (fast, no event replay).
    /// </summary>
    public CustomerReadModel? GetById(Guid id) => projection.GetById(id);

    /// <summary>
    /// Returns all customers from the projection (fast).
    /// </summary>
    public IReadOnlyList<CustomerReadModel> GetAll() => projection.GetAll();

    /// <summary>
    /// Rehydrates the aggregate directly from the event store — demonstrates
    /// that the current state is fully reconstructed by replaying events.
    /// </summary>
    public async Task<CustomerAggregate?> RehydrateAsync(Guid id, CancellationToken cancellationToken = default)
        => await repository.GetByIdAsync(id, cancellationToken);

    private async Task<CustomerAggregate> LoadOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        CustomerAggregate? aggregate = await repository.GetByIdAsync(id, cancellationToken);
        return aggregate ?? throw new KeyNotFoundException($"Customer '{id}' was not found in the event store.");
    }
}
