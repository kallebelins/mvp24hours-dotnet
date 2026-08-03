using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Persistence;

namespace Mvp24Hours.Application.RabbitMQ.Test.Saga;

[Trait("Category", "Unit")]
public class EFCoreSagaRepositoryTest
{
    private static EFCoreSagaRepository<TestOrderSagaData> CreateRepository(FakeSagaDbContext? dbContext = null)
    {
        return new EFCoreSagaRepository<TestOrderSagaData>(
            dbContext ?? new FakeSagaDbContext(),
            NullLogger<EFCoreSagaRepository<TestOrderSagaData>>.Instance);
    }

    [Fact]
    public void Constructor_WithNullDbContext_ShouldThrow()
    {
        Action act = () => new EFCoreSagaRepository<TestOrderSagaData>(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("dbContext");
    }

    [Fact]
    public async Task FindAsync_WhenNotFound_ShouldReturnNull()
    {
        EFCoreSagaRepository<TestOrderSagaData> repository = CreateRepository();
        var correlationId = Guid.NewGuid();

        SagaInstance<TestOrderSagaData>? result = await repository.FindAsync(correlationId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistAndReturnInstance()
    {
        var dbContext = new FakeSagaDbContext();
        EFCoreSagaRepository<TestOrderSagaData> repository = CreateRepository(dbContext);
        var correlationId = Guid.NewGuid();
        var initialData = new TestOrderSagaData { OrderId = "42" };

        SagaInstance<TestOrderSagaData> instance = await repository.CreateAsync(
            correlationId,
            "AwaitingPayment",
            initialData);

        instance.CorrelationId.Should().Be(correlationId);
        instance.CurrentState.Should().Be("AwaitingPayment");
        instance.Data.OrderId.Should().Be("42");
        instance.Version.Should().Be(1);
        instance.StateHistory.Should().ContainSingle(h => h.ToState == "AwaitingPayment");
        dbContext.Entities.Should().ContainKey(correlationId);
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicate_ShouldThrow()
    {
        EFCoreSagaRepository<TestOrderSagaData> repository = CreateRepository();
        var correlationId = Guid.NewGuid();
        await repository.CreateAsync(correlationId, "Initial");

        Func<Task> act = () => repository.CreateAsync(correlationId, "Initial");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task SaveAsync_WithNullInstance_ShouldThrow()
    {
        EFCoreSagaRepository<TestOrderSagaData> repository = CreateRepository();

        Func<Task> act = () => repository.SaveAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SaveAsync_ShouldIncrementVersion()
    {
        EFCoreSagaRepository<TestOrderSagaData> repository = CreateRepository();
        var correlationId = Guid.NewGuid();
        SagaInstance<TestOrderSagaData> instance = await repository.CreateAsync(correlationId, "Initial");
        _ = instance.Version;

        instance.TransitionTo("Shipped");
        int versionBeforeSave = instance.Version;
        await repository.SaveAsync(instance);

        instance.Version.Should().Be(versionBeforeSave + 1);
        SagaInstance<TestOrderSagaData>? loaded = await repository.FindAsync(correlationId);
        loaded!.CurrentState.Should().Be("Shipped");
    }

    [Fact]
    public async Task DeleteAsync_WhenExists_ShouldReturnTrue()
    {
        EFCoreSagaRepository<TestOrderSagaData> repository = CreateRepository();
        var correlationId = Guid.NewGuid();
        await repository.CreateAsync(correlationId, "Initial");

        bool deleted = await repository.DeleteAsync(correlationId);

        deleted.Should().BeTrue();
        (await repository.FindAsync(correlationId)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ShouldReturnFalse()
    {
        EFCoreSagaRepository<TestOrderSagaData> repository = CreateRepository();

        bool deleted = await repository.DeleteAsync(Guid.NewGuid());

        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task FindByStateAsync_ShouldFilterByState()
    {
        EFCoreSagaRepository<TestOrderSagaData> repository = CreateRepository();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        await repository.CreateAsync(id1, "AwaitingPayment");
        await repository.CreateAsync(id2, "Shipped");

        IReadOnlyList<SagaInstance<TestOrderSagaData>> awaiting = await repository.FindByStateAsync("AwaitingPayment");

        awaiting.Should().ContainSingle(s => s.CorrelationId == id1);
    }

    [Fact]
    public async Task FindTimedOutAsync_ShouldReturnStaleInstances()
    {
        var dbContext = new FakeSagaDbContext();
        EFCoreSagaRepository<TestOrderSagaData> repository = CreateRepository(dbContext);
        var correlationId = Guid.NewGuid();
        SagaInstance<TestOrderSagaData> instance = await repository.CreateAsync(correlationId, "AwaitingPayment");
        dbContext.Entities[correlationId].LastUpdatedAt = DateTime.UtcNow.AddHours(-2);

        IReadOnlyList<SagaInstance<TestOrderSagaData>> timedOut =
            await repository.FindTimedOutAsync(TimeSpan.FromHours(1));

        timedOut.Should().ContainSingle(s => s.CorrelationId == correlationId);
    }

    [Fact]
    public async Task FindFaultedAsync_ShouldReturnFaultedInstances()
    {
        EFCoreSagaRepository<TestOrderSagaData> repository = CreateRepository();
        var correlationId = Guid.NewGuid();
        SagaInstance<TestOrderSagaData> instance = await repository.CreateAsync(correlationId, "Initial");
        instance.Fault("payment failed");
        await repository.SaveAsync(instance);

        IReadOnlyList<SagaInstance<TestOrderSagaData>> faulted = await repository.FindFaultedAsync();

        faulted.Should().ContainSingle(s => s.CorrelationId == correlationId);
    }

    [Fact]
    public async Task CleanupAsync_ShouldRemoveOldInstances()
    {
        var dbContext = new FakeSagaDbContext();
        EFCoreSagaRepository<TestOrderSagaData> repository = CreateRepository(dbContext);
        var oldId = Guid.NewGuid();
        var recentId = Guid.NewGuid();
        await repository.CreateAsync(oldId, "Completed");
        await repository.CreateAsync(recentId, "Completed");
        dbContext.Entities[oldId].LastUpdatedAt = DateTime.UtcNow.AddDays(-10);

        int cleaned = await repository.CleanupAsync(TimeSpan.FromDays(5));

        cleaned.Should().Be(1);
        (await repository.FindAsync(oldId)).Should().BeNull();
        (await repository.FindAsync(recentId)).Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ShouldReturnFalse()
    {
        EFCoreSagaRepository<TestOrderSagaData> repository = CreateRepository();

        bool updated = await repository.UpdateAsync(
            Guid.NewGuid(),
            1,
            instance => instance.Data.OrderId = "x");

        updated.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_WithWrongVersion_ShouldReturnFalse()
    {
        EFCoreSagaRepository<TestOrderSagaData> repository = CreateRepository();
        var correlationId = Guid.NewGuid();
        SagaInstance<TestOrderSagaData> saga = await repository.CreateAsync(correlationId, "Initial");

        bool updated = await repository.UpdateAsync(
            correlationId,
            saga.Version + 10,
            instance => instance.Data.OrderId = "x");

        updated.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_WithCorrectVersion_ShouldApplyChanges()
    {
        EFCoreSagaRepository<TestOrderSagaData> repository = CreateRepository();
        var correlationId = Guid.NewGuid();
        SagaInstance<TestOrderSagaData> saga = await repository.CreateAsync(correlationId, "Initial");

        bool updated = await repository.UpdateAsync(
            correlationId,
            saga.Version,
            instance => instance.Data.OrderId = "updated");

        updated.Should().BeTrue();
        SagaInstance<TestOrderSagaData>? loaded = await repository.FindAsync(correlationId);
        loaded!.Data.OrderId.Should().Be("updated");
    }

    [Fact]
    public async Task FindAsync_ShouldRestoreJsonFields()
    {
        var dbContext = new FakeSagaDbContext();
        EFCoreSagaRepository<TestOrderSagaData> repository = CreateRepository(dbContext);
        var correlationId = Guid.NewGuid();
        SagaInstance<TestOrderSagaData> instance = await repository.CreateAsync(correlationId, "Initial");
        instance.Metadata["key"] = "value";
        instance.Errors.Add("err1");
        instance.ScheduledTimeouts.Add(Guid.NewGuid());
        await repository.SaveAsync(instance);

        SagaInstance<TestOrderSagaData>? loaded = await repository.FindAsync(correlationId);

        loaded!.Metadata.Should().ContainKey("key").WhoseValue.Should().Be("value");
        loaded.Errors.Should().Contain("err1");
        loaded.ScheduledTimeouts.Should().NotBeEmpty();
        loaded.StateHistory.Should().NotBeEmpty();
    }

    private sealed class FakeSagaDbContext : ISagaDbContext
    {
        public Dictionary<Guid, SagaStateEntity> Entities { get; } = [];

        private static string SagaTypeName => typeof(TestOrderSagaData).FullName!;

        public Task<SagaStateEntity?> GetSagaStateAsync(Guid correlationId, string sagaTypeName, CancellationToken cancellationToken = default)
        {
            if (sagaTypeName != SagaTypeName)
            {
                return Task.FromResult<SagaStateEntity?>(null);
            }

            Entities.TryGetValue(correlationId, out SagaStateEntity? entity);
            return Task.FromResult(entity);
        }

        public Task AddSagaStateAsync(SagaStateEntity entity, CancellationToken cancellationToken = default)
        {
            Entities[entity.CorrelationId] = entity;
            return Task.CompletedTask;
        }

        public Task UpdateSagaStateAsync(SagaStateEntity entity, CancellationToken cancellationToken = default)
        {
            Entities[entity.CorrelationId] = entity;
            return Task.CompletedTask;
        }

        public Task<bool> DeleteSagaStateAsync(Guid correlationId, string sagaTypeName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Entities.Remove(correlationId));
        }

        public Task<IReadOnlyList<SagaStateEntity>> GetSagasByStateAsync(string sagaTypeName, string state, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<SagaStateEntity> result = [.. Entities.Values.Where(e => e.SagaTypeName == sagaTypeName && e.CurrentState == state)];
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<SagaStateEntity>> GetTimedOutSagasAsync(string sagaTypeName, DateTime threshold, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<SagaStateEntity> result = [.. Entities.Values.Where(e => e.SagaTypeName == sagaTypeName && e.LastUpdatedAt < threshold)];
            return Task.FromResult(result);
        }

        public Task<int> CleanupOldSagasAsync(string sagaTypeName, DateTime threshold, CancellationToken cancellationToken = default)
        {
            List<Guid> toRemove = [.. Entities.Values
                .Where(e => e.SagaTypeName == sagaTypeName && e.LastUpdatedAt < threshold)
                .Select(e => e.CorrelationId)];
            foreach (Guid id in toRemove)
            {
                Entities.Remove(id);
            }

            return Task.FromResult(toRemove.Count);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(1);
        }
    }
}
