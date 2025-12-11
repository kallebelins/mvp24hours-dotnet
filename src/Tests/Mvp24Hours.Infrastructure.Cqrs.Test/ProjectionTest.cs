//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Infrastructure.Cqrs.EventSourcing;
using Mvp24Hours.Infrastructure.Cqrs.Projections;
using Xunit;

namespace Mvp24Hours.Infrastructure.Cqrs.Test;

public class ProjectionTest
{
    #region [ Read Model Repository Tests ]

    [Fact]
    public async Task InMemoryReadModelRepository_InsertAndGetById_ShouldWork()
    {
        // Arrange
        var repository = new InMemoryReadModelRepository<TestReadModel>();
        var model = new TestReadModel { Id = Guid.NewGuid(), Name = "Test", Status = "Active" };

        // Act
        await repository.InsertAsync(model);
        var result = await repository.GetByIdAsync(model.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(model.Id, result.Id);
        Assert.Equal(model.Name, result.Name);
    }

    [Fact]
    public async Task InMemoryReadModelRepository_Upsert_ShouldInsertOrUpdate()
    {
        // Arrange
        var repository = new InMemoryReadModelRepository<TestReadModel>();
        var id = Guid.NewGuid();
        var model1 = new TestReadModel { Id = id, Name = "Test1", Status = "Active" };
        var model2 = new TestReadModel { Id = id, Name = "Test2", Status = "Updated" };

        // Act
        await repository.UpsertAsync(model1);
        await repository.UpsertAsync(model2);
        var result = await repository.GetByIdAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test2", result.Name);
        Assert.Equal("Updated", result.Status);
    }

    [Fact]
    public async Task InMemoryReadModelRepository_Find_ShouldReturnMatchingItems()
    {
        // Arrange
        var repository = new InMemoryReadModelRepository<TestReadModel>();
        await repository.InsertAsync(new TestReadModel { Id = Guid.NewGuid(), Name = "Item1", Status = "Active" });
        await repository.InsertAsync(new TestReadModel { Id = Guid.NewGuid(), Name = "Item2", Status = "Active" });
        await repository.InsertAsync(new TestReadModel { Id = Guid.NewGuid(), Name = "Item3", Status = "Inactive" });

        // Act
        var activeItems = await repository.FindAsync(x => x.Status == "Active");

        // Assert
        Assert.Equal(2, activeItems.Count);
        Assert.All(activeItems, item => Assert.Equal("Active", item.Status));
    }

    [Fact]
    public async Task InMemoryReadModelRepository_FindWithPaging_ShouldPageResults()
    {
        // Arrange
        var repository = new InMemoryReadModelRepository<TestReadModel>();
        for (int i = 0; i < 10; i++)
        {
            await repository.InsertAsync(new TestReadModel 
            { 
                Id = Guid.NewGuid(), 
                Name = $"Item{i}", 
                Status = "Active" 
            });
        }

        // Act
        var page1 = await repository.FindAsync(x => true, skip: 0, take: 3);
        var page2 = await repository.FindAsync(x => true, skip: 3, take: 3);

        // Assert
        Assert.Equal(3, page1.Count);
        Assert.Equal(3, page2.Count);
    }

    [Fact]
    public async Task InMemoryReadModelRepository_Count_ShouldReturnCorrectCount()
    {
        // Arrange
        var repository = new InMemoryReadModelRepository<TestReadModel>();
        await repository.InsertAsync(new TestReadModel { Id = Guid.NewGuid(), Name = "Item1", Status = "Active" });
        await repository.InsertAsync(new TestReadModel { Id = Guid.NewGuid(), Name = "Item2", Status = "Active" });
        await repository.InsertAsync(new TestReadModel { Id = Guid.NewGuid(), Name = "Item3", Status = "Inactive" });

        // Act
        var totalCount = await repository.CountAsync();
        var activeCount = await repository.CountAsync(x => x.Status == "Active");

        // Assert
        Assert.Equal(3, totalCount);
        Assert.Equal(2, activeCount);
    }

    [Fact]
    public async Task InMemoryReadModelRepository_BulkInsert_ShouldInsertAll()
    {
        // Arrange
        var repository = new InMemoryReadModelRepository<TestReadModel>();
        var items = Enumerable.Range(0, 100)
            .Select(i => new TestReadModel { Id = Guid.NewGuid(), Name = $"Item{i}", Status = "Active" })
            .ToList();

        // Act
        await repository.BulkInsertAsync(items);
        var count = await repository.CountAsync();

        // Assert
        Assert.Equal(100, count);
    }

    [Fact]
    public async Task InMemoryReadModelRepository_DeleteAll_ShouldClearAllData()
    {
        // Arrange
        var repository = new InMemoryReadModelRepository<TestReadModel>();
        await repository.InsertAsync(new TestReadModel { Id = Guid.NewGuid(), Name = "Item1", Status = "Active" });
        await repository.InsertAsync(new TestReadModel { Id = Guid.NewGuid(), Name = "Item2", Status = "Active" });

        // Act
        await repository.DeleteAllAsync();
        var count = await repository.CountAsync();

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task InMemoryReadModelRepository_Exists_ShouldReturnTrueIfExists()
    {
        // Arrange
        var repository = new InMemoryReadModelRepository<TestReadModel>();
        await repository.InsertAsync(new TestReadModel { Id = Guid.NewGuid(), Name = "Item1", Status = "Active" });

        // Act
        var exists = await repository.ExistsAsync(x => x.Name == "Item1");
        var notExists = await repository.ExistsAsync(x => x.Name == "NonExistent");

        // Assert
        Assert.True(exists);
        Assert.False(notExists);
    }

    #endregion

    #region [ Projection Handler Tests ]

    [Fact]
    public async Task ProjectionHandler_ShouldHandleEvent()
    {
        // Arrange
        var repository = new InMemoryReadModelRepository<TestReadModel>();
        var handler = new TestProjectionHandler(repository);
        var @event = new TestProjectionEvent(Guid.NewGuid(), "Test Item");
        var context = new ProjectionContext
        {
            GlobalPosition = 1,
            AggregateId = Guid.NewGuid(),
            Version = 1,
            Timestamp = DateTime.UtcNow,
            ProjectionName = "TestProjection"
        };

        // Act
        await handler.HandleAsync(@event, context);

        // Assert
        var result = await repository.GetByIdAsync(@event.ItemId);
        Assert.NotNull(result);
        Assert.Equal(@event.Name, result.Name);
    }

    [Fact]
    public void ProjectionHandler_ShouldReportHandledEventTypes()
    {
        // Arrange
        var repository = new InMemoryReadModelRepository<TestReadModel>();
        var handler = new TestProjectionHandler(repository);

        // Act
        IProjectionHandler projectionHandler = handler;
        var handledTypes = projectionHandler.HandledEventTypes;

        // Assert
        Assert.Single(handledTypes);
        Assert.Equal(typeof(TestProjectionEvent), handledTypes[0]);
    }

    #endregion

    #region [ Projection Position Store Tests ]

    [Fact]
    public async Task InMemoryProjectionPositionStore_SaveAndGetPosition_ShouldWork()
    {
        // Arrange
        var store = new InMemoryProjectionPositionStore();
        var projectionName = "TestProjection";

        // Act
        await store.SavePositionAsync(projectionName, 100);
        var position = await store.GetPositionAsync(projectionName);

        // Assert
        Assert.Equal(100, position);
    }

    [Fact]
    public async Task InMemoryProjectionPositionStore_UnknownProjection_ShouldReturnZero()
    {
        // Arrange
        var store = new InMemoryProjectionPositionStore();

        // Act
        var position = await store.GetPositionAsync("UnknownProjection");

        // Assert
        Assert.Equal(0, position);
    }

    #endregion

    #region [ Projection Context Tests ]

    [Fact]
    public void ProjectionContext_FromStoredEvent_ShouldMapCorrectly()
    {
        // Arrange
        var storedEvent = new StoredEvent
        {
            Id = Guid.NewGuid(),
            GlobalPosition = 42,
            AggregateId = Guid.NewGuid(),
            Version = 5,
            Timestamp = DateTime.UtcNow,
            CorrelationId = "correlation-123",
            CausationId = "causation-456"
        };

        // Act
        var context = ProjectionContext.FromStoredEvent(storedEvent, "TestProjection", isRebuilding: true);

        // Assert
        Assert.Equal(42, context.GlobalPosition);
        Assert.Equal(storedEvent.AggregateId, context.AggregateId);
        Assert.Equal(5, context.Version);
        Assert.Equal("correlation-123", context.CorrelationId);
        Assert.Equal("causation-456", context.CausationId);
        Assert.Equal("TestProjection", context.ProjectionName);
        Assert.True(context.IsRebuilding);
    }

    #endregion

    #region [ Projection Status Tests ]

    [Fact]
    public void ProjectionBase_StatusTransitions_ShouldWork()
    {
        // Arrange
        var projection = new ConcreteProjection();

        // Act & Assert
        Assert.Equal(ProjectionStatus.NotStarted, projection.Status);

        projection.SetCatchingUp();
        Assert.Equal(ProjectionStatus.CatchingUp, projection.Status);

        projection.SetLive();
        Assert.Equal(ProjectionStatus.Live, projection.Status);

        projection.SetRebuilding();
        Assert.Equal(ProjectionStatus.Rebuilding, projection.Status);

        projection.SetFaulted(new InvalidOperationException("Test error"));
        Assert.Equal(ProjectionStatus.Faulted, projection.Status);
        Assert.NotNull(projection.LastError);

        projection.SetStopped();
        Assert.Equal(ProjectionStatus.Stopped, projection.Status);
    }

    [Fact]
    public async Task ProjectionBase_Reset_ShouldClearState()
    {
        // Arrange
        var projection = new ConcreteProjection();
        await projection.UpdatePositionAsync(100);
        projection.SetLive();
        projection.SetFaulted(new Exception("Error"));

        // Act
        await projection.ResetAsync();

        // Assert
        Assert.Equal(0, projection.Position);
        Assert.Equal(ProjectionStatus.NotStarted, projection.Status);
        Assert.Null(projection.LastError);
        Assert.Null(projection.LastUpdatedAt);
    }

    [Fact]
    public async Task ProjectionBase_UpdatePosition_ShouldSetPositionAndTimestamp()
    {
        // Arrange
        var projection = new ConcreteProjection();

        // Act
        await projection.UpdatePositionAsync(50);

        // Assert
        Assert.Equal(50, projection.Position);
        Assert.NotNull(projection.LastUpdatedAt);
    }

    #endregion

    #region [ Checkpoint Manager Tests ]

    [Fact]
    public async Task InMemoryProjectionCheckpointManager_SaveAndGet_ShouldWork()
    {
        // Arrange
        var manager = new InMemoryProjectionCheckpointManager();
        var checkpoint = new ProjectionCheckpoint
        {
            ProjectionName = "TestProjection",
            Position = 100,
            Timestamp = DateTime.UtcNow
        };

        // Act
        await manager.SaveCheckpointAsync(checkpoint);
        var result = await manager.GetCheckpointAsync("TestProjection");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result.Position);
        Assert.Equal("TestProjection", result.ProjectionName);
    }

    [Fact]
    public async Task InMemoryProjectionCheckpointManager_UnknownProjection_ShouldReturnNull()
    {
        // Arrange
        var manager = new InMemoryProjectionCheckpointManager();

        // Act
        var result = await manager.GetCheckpointAsync("UnknownProjection");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region [ DI Registration Tests ]

    [Fact]
    public void AddProjections_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEventSourcingInMemory();

        // Act
        services.AddProjections(options =>
        {
            options.AddInMemoryRepository<TestReadModel>();
        });

        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<IProjectionPositionStore>());
        Assert.NotNull(provider.GetService<IProjectionManager>());
        Assert.NotNull(provider.GetService<IProjectionRebuildService>());
        Assert.NotNull(provider.GetService<IReadModelRepository<TestReadModel>>());
    }

    [Fact]
    public void AddInMemoryReadModelRepository_ShouldRegisterRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInMemoryReadModelRepository<TestReadModel>();

        var provider = services.BuildServiceProvider();
        var repository = provider.GetService<IReadModelRepository<TestReadModel>>();

        // Assert
        Assert.NotNull(repository);
        Assert.IsType<InMemoryReadModelRepository<TestReadModel>>(repository);
    }

    #endregion

    #region [ Support Classes ]

    public class TestReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public record TestProjectionEvent(Guid ItemId, string Name) : DomainEventBase;

    public class TestProjectionHandler : IProjectionHandler<TestProjectionEvent>
    {
        private readonly IReadModelRepository<TestReadModel> _repository;

        public TestProjectionHandler(IReadModelRepository<TestReadModel> repository)
        {
            _repository = repository;
        }

        public async Task HandleAsync(TestProjectionEvent @event, ProjectionContext context, CancellationToken cancellationToken = default)
        {
            await _repository.UpsertAsync(new TestReadModel
            {
                Id = @event.ItemId,
                Name = @event.Name,
                Status = "Active",
                CreatedAt = @event.OccurredAt
            }, cancellationToken);
        }
    }

    public class ConcreteProjection : ProjectionBase
    {
        public override string Name => "ConcreteProjection";
    }

    #endregion
}


