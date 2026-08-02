//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Infrastructure.Cqrs.EventSourcing;
using Mvp24Hours.Infrastructure.Cqrs.Projections;
using Mvp24Hours.Infrastructure.Cqrs.Test.Support;

namespace Mvp24Hours.Infrastructure.Cqrs.Test.Projections;

[Trait("Category", "Unit")]
public class ProjectionsAdvancedTest
{
    [Fact]
    public async Task ProjectionManager_ProcessEventAsync_ShouldUpdateReadModelAndPosition()
    {
        ProjectionTestInfrastructure infra = CreateInfrastructure();
        infra.Manager.RegisterProjection("OrderProjection", typeof(OrderSummaryProjectionHandler));

        var aggregateId = Guid.NewGuid();
        await infra.EventStore.AppendEventsAsync(
            aggregateId,
            [new OrderCreatedEvent { OrderId = aggregateId, CustomerEmail = "a@b.com", TotalAmount = 10 }],
            0);

        StoredEvent storedEvent = infra.EventStore.GetAllStoredEvents().Single();

        await infra.Manager.ProcessEventAsync(storedEvent);

        OrderSummaryReadModel? model = await infra.Repository.GetByIdAsync(aggregateId);
        ProjectionInfo? info = infra.Manager.GetProjectionInfo("OrderProjection");

        Assert.NotNull(model);
        Assert.Equal("a@b.com", model.CustomerEmail);
        Assert.NotNull(info);
        Assert.Equal(storedEvent.GlobalPosition, info!.Position);
    }

    [Fact]
    public async Task ProjectionManager_RebuildAsync_UnknownProjection_ShouldThrow()
    {
        ProjectionTestInfrastructure infra = CreateInfrastructure();

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            infra.Manager.RebuildAsync("UnknownProjection"));

        Assert.Contains("UnknownProjection", ex.Message);
    }

    [Fact]
    public async Task ProjectionRebuildService_ScheduleRebuildAsync_ShouldMarkCompleted()
    {
        var manager = new MockProjectionManager();
        var rebuildService = new ProjectionRebuildService(manager, NullLogger<ProjectionRebuildService>.Instance);

        await rebuildService.ScheduleRebuildAsync("OrderProjection");

        RebuildStatus? status = rebuildService.GetRebuildStatus("OrderProjection");
        Assert.NotNull(status);
        Assert.Equal(RebuildState.Completed, status!.State);
        Assert.True(manager.RebuildCalled);
    }

    [Fact]
    public void ProjectionManager_GetProjectionInfos_ShouldReturnRegisteredProjections()
    {
        ProjectionTestInfrastructure infra = CreateInfrastructure();
        infra.Manager.RegisterProjection("OrderProjection", typeof(OrderSummaryProjectionHandler));

        IReadOnlyList<ProjectionInfo> infos = infra.Manager.GetProjectionInfos();

        Assert.Single(infos);
        Assert.Equal("OrderProjection", infos[0].Name);
        Assert.Contains(nameof(OrderCreatedEvent), infos[0].HandledEventTypes);
    }

    [Fact]
    public async Task ProjectionHostedService_StopAsync_ShouldStopProjectionManager()
    {
        var manager = new MockProjectionManager();
        var hostedService = new ProjectionHostedService(manager, NullLogger<ProjectionHostedService>.Instance);

        using var cts = new CancellationTokenSource(100);
        await hostedService.StartAsync(CancellationToken.None);
        cts.Cancel();
        await hostedService.StopAsync(CancellationToken.None);

        Assert.True(manager.StopCalled);
    }

    [Fact]
    public async Task IncrementalProjection_ResetAsync_ShouldClearRepository()
    {
        var repository = new InMemoryReadModelRepository<OrderSummaryReadModel>();
        var projection = new TestIncrementalProjection(repository);
        await repository.InsertAsync(new OrderSummaryReadModel { Id = Guid.NewGuid(), CustomerEmail = "x" });

        await projection.ResetAsync();

        Assert.Equal(0, await repository.CountAsync());
        Assert.Equal(ProjectionStatus.NotStarted, projection.Status);
    }

    [Fact]
    public async Task ApplyProjection_ShouldDispatchApplyMethods()
    {
        var repository = new InMemoryReadModelRepository<OrderSummaryReadModel>();
        var projection = new TestApplyProjection(repository);
        var aggregateId = Guid.NewGuid();
        var context = new ProjectionContext
        {
            GlobalPosition = 1,
            AggregateId = aggregateId,
            ProjectionName = "ApplyProjection"
        };

        await projection.ProcessEventAsync(
            new OrderCreatedEvent { OrderId = aggregateId, CustomerEmail = "apply@test.com", TotalAmount = 1 },
            context);

        OrderSummaryReadModel? model = await repository.GetByIdAsync(aggregateId);
        Assert.NotNull(model);
        Assert.Equal("apply@test.com", model!.CustomerEmail);
    }

    [Fact]
    public async Task BatchProjection_ProcessBatchAsync_ShouldHandleMultipleEvents()
    {
        var repository = new InMemoryReadModelRepository<OrderSummaryReadModel>();
        var projection = new TestBatchProjection(repository);
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        await projection.ProcessBatchAsync(
        [
            (new OrderCreatedEvent { OrderId = id1, CustomerEmail = "one@test.com", TotalAmount = 1 },
                new ProjectionContext { GlobalPosition = 1, AggregateId = id1, ProjectionName = "Batch" }),
            (new OrderCreatedEvent { OrderId = id2, CustomerEmail = "two@test.com", TotalAmount = 2 },
                new ProjectionContext { GlobalPosition = 2, AggregateId = id2, ProjectionName = "Batch" })
        ]);

        Assert.Equal(2, await repository.CountAsync());
    }

    [Fact]
    public async Task AggregatingProjectionHandler_ShouldHandleRegisteredEvents()
    {
        var repository = new InMemoryReadModelRepository<OrderSummaryReadModel>();
        var handler = new TestAggregatingProjectionHandler(repository);
        var aggregateId = Guid.NewGuid();

        await handler.HandleAsync(
            new OrderCreatedEvent { OrderId = aggregateId, CustomerEmail = "agg@test.com", TotalAmount = 1 },
            new ProjectionContext { GlobalPosition = 1, AggregateId = aggregateId, ProjectionName = "Agg" });

        OrderSummaryReadModel? model = await repository.GetByIdAsync(aggregateId);
        Assert.NotNull(model);
        Assert.True(handler.CanHandle(typeof(OrderCreatedEvent)));
        Assert.False(handler.CanHandle(typeof(OrderPaidEvent)));
    }

    [Fact]
    public async Task ReadModelProjectionHandler_ShouldExposeRepository()
    {
        var repository = new InMemoryReadModelRepository<OrderSummaryReadModel>();
        var handler = new TestReadModelProjectionHandler(repository);
        var aggregateId = Guid.NewGuid();

        await handler.HandleAsync(
            new OrderCreatedEvent { OrderId = aggregateId, CustomerEmail = "read@test.com", TotalAmount = 1 },
            new ProjectionContext { GlobalPosition = 1, AggregateId = aggregateId, ProjectionName = "ReadModel" });

        OrderSummaryReadModel? model = await repository.GetByIdAsync(aggregateId);
        Assert.NotNull(model);
        Assert.Equal("read@test.com", model!.CustomerEmail);
    }

    [Fact]
    public void AddProjectionHostedService_ShouldRegisterHostedService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEventSourcingInMemory();
        services.AddProjections();
        services.AddProjectionHostedService();

        ServiceProvider provider = services.BuildServiceProvider();
        IEnumerable<IHostedService> hostedServices = provider.GetServices<IHostedService>();

        Assert.Contains(hostedServices, s => s is ProjectionHostedService);
    }

    private static ProjectionTestInfrastructure CreateInfrastructure()
    {
        var eventStore = new InMemoryEventStore();
        var serializer = new JsonEventSerializer();
        var repository = new InMemoryReadModelRepository<OrderSummaryReadModel>();
        var services = new ServiceCollection();
        services.AddSingleton<IReadModelRepository<OrderSummaryReadModel>>(repository);
        services.AddTransient<OrderSummaryProjectionHandler>();
        ServiceProvider sp = services.BuildServiceProvider();

        var manager = new ProjectionManager(
            eventStore,
            serializer,
            sp,
            new InMemoryProjectionPositionStore(),
            NullLogger<ProjectionManager>.Instance);

        return new ProjectionTestInfrastructure(eventStore, repository, manager);
    }

    private sealed record ProjectionTestInfrastructure(
        InMemoryEventStore EventStore,
        InMemoryReadModelRepository<OrderSummaryReadModel> Repository,
        ProjectionManager Manager);

    public sealed class OrderSummaryReadModel
    {
        public Guid Id { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public sealed class OrderSummaryProjectionHandler(IReadModelRepository<OrderSummaryReadModel> repository)
        : ReadModelProjectionHandler<OrderCreatedEvent, OrderSummaryReadModel>(repository)
    {
        public override async Task HandleAsync(
            OrderCreatedEvent @event,
            ProjectionContext context,
            CancellationToken cancellationToken = default)
        {
            await Repository.UpsertAsync(new OrderSummaryReadModel
            {
                Id = @event.OrderId,
                CustomerEmail = @event.CustomerEmail,
                Status = "Created"
            }, cancellationToken);
        }
    }

    private sealed class TestIncrementalProjection(IReadModelRepository<OrderSummaryReadModel> repository)
        : IncrementalProjection<OrderSummaryReadModel>(repository)
    {
        public override string Name => "IncrementalTest";

        protected override void RegisterEventHandlers()
        {
            Handles<OrderCreatedEvent>();
        }

        public override Task ProcessEventAsync(
            IMediatorDomainEvent @event,
            ProjectionContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestApplyProjection(IReadModelRepository<OrderSummaryReadModel> repository)
        : ApplyProjection<OrderSummaryReadModel>(repository)
    {
        public override string Name => "ApplyTest";

        protected override void RegisterEventHandlers()
        {
            Handles<OrderCreatedEvent>();
        }

        private Task Apply(
            OrderCreatedEvent @event,
            ProjectionContext context,
            CancellationToken cancellationToken)
        {
            return Repository.UpsertAsync(new OrderSummaryReadModel
            {
                Id = @event.OrderId,
                CustomerEmail = @event.CustomerEmail,
                Status = "Created"
            }, cancellationToken);
        }
    }

    private sealed class TestBatchProjection(IReadModelRepository<OrderSummaryReadModel> repository)
        : BatchProjection<OrderSummaryReadModel>(repository)
    {
        public override string Name => "BatchTest";

        public override async Task ProcessBatchAsync(
            IReadOnlyList<(IMediatorDomainEvent Event, ProjectionContext Context)> events,
            CancellationToken cancellationToken = default)
        {
            foreach ((IMediatorDomainEvent @event, _) in events)
            {
                if (@event is OrderCreatedEvent created)
                {
                    await Repository.UpsertAsync(new OrderSummaryReadModel
                    {
                        Id = created.OrderId,
                        CustomerEmail = created.CustomerEmail,
                        Status = "Created"
                    }, cancellationToken);
                }
            }
        }
    }

    private sealed class TestAggregatingProjectionHandler : AggregatingProjectionHandler<OrderSummaryReadModel>
    {
        public TestAggregatingProjectionHandler(IReadModelRepository<OrderSummaryReadModel> repository) : base(repository)
        {
            Handles<OrderCreatedEvent>();
        }

        public override async Task HandleAsync(
            Core.Contract.Domain.Entity.IDomainEvent @event,
            ProjectionContext context,
            CancellationToken cancellationToken = default)
        {
            if (@event is OrderCreatedEvent created)
            {
                await Repository.UpsertAsync(new OrderSummaryReadModel
                {
                    Id = created.OrderId,
                    CustomerEmail = created.CustomerEmail,
                    Status = "Created"
                }, cancellationToken);
            }
        }
    }

    private sealed class TestReadModelProjectionHandler(IReadModelRepository<OrderSummaryReadModel> repository)
        : ReadModelProjectionHandler<OrderCreatedEvent, OrderSummaryReadModel>(repository)
    {
        public override Task HandleAsync(
            OrderCreatedEvent @event,
            ProjectionContext context,
            CancellationToken cancellationToken = default)
        {
            return Repository.UpsertAsync(new OrderSummaryReadModel
            {
                Id = @event.OrderId,
                CustomerEmail = @event.CustomerEmail,
                Status = "Created"
            }, cancellationToken);
        }
    }

    private sealed class MockProjectionManager : IProjectionManager
    {
        public bool StopCalled { get; private set; }

        public bool RebuildCalled { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            StopCalled = true;
            return Task.CompletedTask;
        }

        public Task RebuildAsync(string projectionName, CancellationToken cancellationToken = default)
        {
            RebuildCalled = true;
            return Task.CompletedTask;
        }

        public Task RebuildAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public IReadOnlyList<ProjectionInfo> GetProjectionInfos()
        {
            return [];
        }

        public ProjectionInfo? GetProjectionInfo(string projectionName)
        {
            return null;
        }

        public Task ProcessEventAsync(StoredEvent storedEvent, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
