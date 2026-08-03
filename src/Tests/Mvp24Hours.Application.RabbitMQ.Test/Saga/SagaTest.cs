using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Persistence;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing;

namespace Mvp24Hours.Application.RabbitMQ.Test.Saga;

[Trait("Category", "Unit")]
public class SagaTest
{
    private static RedisSagaRepository<TestOrderSagaData> CreateRedisRepository(
        TimeSpan? defaultExpiration = null)
    {
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var options = new RedisSagaRepositoryOptions
        {
            DefaultExpiration = defaultExpiration ?? TimeSpan.FromHours(24),
            CompletedExpiration = TimeSpan.FromHours(1)
        };
        return new RedisSagaRepository<TestOrderSagaData>(cache, options);
    }

    private static string RedisSagaKey(Guid correlationId)
    {
        return $"saga:{typeof(TestOrderSagaData).Name}:{correlationId}";
    }

    [Fact]
    public void SagaInstance_TransitionTo_ShouldUpdateStateAndHistory()
    {
        var saga = new SagaInstance<TestOrderSagaData>();

        saga.TransitionTo("AwaitingPayment", "order created");

        saga.CurrentState.Should().Be("AwaitingPayment");
        saga.Version.Should().Be(1);
        saga.StateHistory.Should().ContainSingle(h => h.ToState == "AwaitingPayment");
        saga.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SagaInstance_Complete_ShouldMarkCompleted()
    {
        var saga = new SagaInstance<TestOrderSagaData>();
        saga.TransitionTo("Shipped");

        saga.Complete();

        saga.IsCompleted.Should().BeTrue();
        saga.IsActive.Should().BeFalse();
        saga.CompletedAt.Should().NotBeNull();
        saga.CurrentState.Should().Be("Completed");
    }

    [Fact]
    public void SagaInstance_Fault_ShouldRecordError()
    {
        var saga = new SagaInstance<TestOrderSagaData>();

        saga.Fault("payment failed");

        saga.IsFaulted.Should().BeTrue();
        saga.ErrorMessage.Should().Be("payment failed");
        saga.Errors.Should().ContainSingle(e => e.Contains("payment failed"));
    }

    [Fact]
    public async Task InMemorySagaRepository_CreateDuplicate_ShouldThrow()
    {
        var repository = new InMemorySagaRepository<TestOrderSagaData>();
        var correlationId = Guid.NewGuid();

        await repository.CreateAsync(correlationId, "Initial");

        Func<Task> act = () => repository.CreateAsync(correlationId, "Initial");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task InMemorySagaRepository_UpdateAsync_WithWrongVersion_ShouldReturnFalse()
    {
        var repository = new InMemorySagaRepository<TestOrderSagaData>();
        var correlationId = Guid.NewGuid();
        SagaInstance<TestOrderSagaData> saga = await repository.CreateAsync(correlationId, "Initial");

        bool updated = await repository.UpdateAsync(
            correlationId,
            expectedVersion: saga.Version + 10,
            instance => instance.Data.OrderId = "x");

        updated.Should().BeFalse();
    }

    [Fact]
    public async Task InMemorySagaRepository_FindByStateAsync_ShouldFilter()
    {
        var repository = new InMemorySagaRepository<TestOrderSagaData>();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        await repository.CreateAsync(id1, "AwaitingPayment");
        await repository.CreateAsync(id2, "Shipped");

        IReadOnlyList<SagaInstance<TestOrderSagaData>> awaiting = await repository.FindByStateAsync("AwaitingPayment");

        awaiting.Should().ContainSingle(s => s.CorrelationId == id1);
    }

    [Fact]
    public async Task OrderSagaStateMachine_InitialEvent_ShouldTransitionState()
    {
        var machine = new OrderSagaStateMachine();
        var saga = new SagaInstance<TestOrderSagaData> { CorrelationId = Guid.NewGuid(), CurrentState = machine.InitialState };
        TestConsumeContext<TestOrderCreatedEvent> context = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestOrderCreatedEvent { CorrelationId = saga.CorrelationId, OrderId = "42" });

        bool handled = await machine.ProcessEventAsync(
            saga,
            new TestOrderCreatedEvent { CorrelationId = saga.CorrelationId, OrderId = "42" },
            context);

        handled.Should().BeTrue();
        saga.CurrentState.Should().Be("AwaitingPayment");
        saga.Data.OrderId.Should().Be("42");
    }

    [Fact]
    public async Task OrderSagaStateMachine_PaymentCompleted_ShouldComplete()
    {
        var machine = new OrderSagaStateMachine();
        var saga = new SagaInstance<TestOrderSagaData>
        {
            CorrelationId = Guid.NewGuid(),
            CurrentState = "AwaitingPayment",
            Data = new TestOrderSagaData { OrderId = "42" }
        };
        TestConsumeContext<TestPaymentCompletedEvent> context = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestPaymentCompletedEvent { CorrelationId = saga.CorrelationId });

        await machine.ProcessEventAsync(
            saga,
            new TestPaymentCompletedEvent { CorrelationId = saga.CorrelationId },
            context);

        saga.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task InMemorySagaRepository_CreateAndFind_ShouldReturnSaga()
    {
        var repository = new InMemorySagaRepository<TestOrderSagaData>();
        var id = Guid.NewGuid();

        SagaInstance<TestOrderSagaData> created = await repository.CreateAsync(id, "Initial");
        SagaInstance<TestOrderSagaData>? retrieved = await repository.FindAsync(id);

        retrieved.Should().NotBeNull();
        retrieved!.CorrelationId.Should().Be(id);
        retrieved.CurrentState.Should().Be("Initial");
        created.Should().NotBeNull();
    }

    [Fact]
    public async Task InMemorySagaRepository_FindNonExistent_ShouldReturnNull()
    {
        var repository = new InMemorySagaRepository<TestOrderSagaData>();

        SagaInstance<TestOrderSagaData>? result = await repository.FindAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task InMemorySagaRepository_DeleteAsync_ShouldRemoveSaga()
    {
        var repository = new InMemorySagaRepository<TestOrderSagaData>();
        var id = Guid.NewGuid();
        await repository.CreateAsync(id, "Initial");

        bool deleted = await repository.DeleteAsync(id);
        SagaInstance<TestOrderSagaData>? result = await repository.FindAsync(id);

        deleted.Should().BeTrue();
        result.Should().BeNull();
    }

    [Fact]
    public async Task InMemorySagaRepository_UpdateAsync_ShouldModifyData()
    {
        var repository = new InMemorySagaRepository<TestOrderSagaData>();
        var id = Guid.NewGuid();
        SagaInstance<TestOrderSagaData> saga = await repository.CreateAsync(id, "Initial");

        bool updated = await repository.UpdateAsync(
            id,
            expectedVersion: saga.Version,
            instance => instance.Data.OrderId = "updated-order");

        SagaInstance<TestOrderSagaData>? result = await repository.FindAsync(id);
        updated.Should().BeTrue();
        result!.Data.OrderId.Should().Be("updated-order");
    }

    [Fact]
    public void SagaInstance_MultipleTransitions_ShouldAccumulateHistory()
    {
        var saga = new SagaInstance<TestOrderSagaData>();

        saga.TransitionTo("AwaitingPayment");
        saga.TransitionTo("Shipped");
        saga.TransitionTo("Delivered");

        saga.StateHistory.Should().HaveCount(3);
        saga.Version.Should().Be(3);
    }

    [Fact]
    public void SagaInstance_AddError_ShouldAccumulateErrors()
    {
        var saga = new SagaInstance<TestOrderSagaData>();

        saga.Fault("error one");
        saga.Fault("error two");

        saga.Errors.Should().HaveCount(2);
        saga.IsFaulted.Should().BeTrue();
    }

    [Fact]
    public void SagaInstance_ScheduledTimeouts_InitiallyEmpty()
    {
        var saga = new SagaInstance<TestOrderSagaData>();

        saga.ScheduledTimeouts.Should().BeEmpty();
    }

    [Fact]
    public void SagaInstance_Metadata_ShouldStoreAndRetrieve()
    {
        var saga = new SagaInstance<TestOrderSagaData>();

        saga.Metadata["key1"] = "value1";

        saga.Metadata["key1"].Should().Be("value1");
    }

    [Fact]
    public async Task SagaConsumeContext_TransitionToAsync_ShouldUpdateState()
    {
        TestConsumeContext<TestOrderCreatedEvent> innerContext = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestOrderCreatedEvent { OrderId = "42", CorrelationId = Guid.NewGuid() });
        var sagaInstance = new SagaInstance<TestOrderSagaData>();
        var ctx = new SagaConsumeContext<TestOrderSagaData, TestOrderCreatedEvent>(
            innerContext, sagaInstance, isNew: true);

        await ctx.TransitionToAsync("Processing");

        ctx.CurrentState.Should().Be("Processing");
        sagaInstance.CurrentState.Should().Be("Processing");
    }

    [Fact]
    public async Task SagaConsumeContext_CompleteAsync_ShouldMarkCompleted()
    {
        TestConsumeContext<TestOrderCreatedEvent> innerContext = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestOrderCreatedEvent { OrderId = "42", CorrelationId = Guid.NewGuid() });
        var sagaInstance = new SagaInstance<TestOrderSagaData>();
        var ctx = new SagaConsumeContext<TestOrderSagaData, TestOrderCreatedEvent>(
            innerContext, sagaInstance, isNew: true);

        await ctx.CompleteAsync();

        ctx.IsCompleted.Should().BeTrue();
        sagaInstance.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task SagaConsumeContext_FaultAsync_ShouldMarkFaulted()
    {
        TestConsumeContext<TestOrderCreatedEvent> innerContext = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestOrderCreatedEvent { OrderId = "42", CorrelationId = Guid.NewGuid() });
        var sagaInstance = new SagaInstance<TestOrderSagaData>();
        var ctx = new SagaConsumeContext<TestOrderSagaData, TestOrderCreatedEvent>(
            innerContext, sagaInstance, isNew: true);

        await ctx.FaultAsync("payment error");

        ctx.IsFaulted.Should().BeTrue();
        sagaInstance.IsFaulted.Should().BeTrue();
        sagaInstance.ErrorMessage.Should().Be("payment error");
    }

    [Fact]
    public void SagaConsumeContext_SetAndGetMetadata_ShouldWork()
    {
        TestConsumeContext<TestOrderCreatedEvent> innerContext = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestOrderCreatedEvent { OrderId = "42", CorrelationId = Guid.NewGuid() });
        var sagaInstance = new SagaInstance<TestOrderSagaData>();
        var ctx = new SagaConsumeContext<TestOrderSagaData, TestOrderCreatedEvent>(
            innerContext, sagaInstance, isNew: true);

        ctx.SetMetadata("trackingId", "TRK-123");

        ctx.GetMetadata("trackingId").Should().Be("TRK-123");
        ctx.GetMetadata("missing").Should().BeNull();
    }

    [Fact]
    public void SagaConsumeContext_IsNew_ShouldReflectConstructorValue()
    {
        TestConsumeContext<TestOrderCreatedEvent> innerContext = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestOrderCreatedEvent { OrderId = "42", CorrelationId = Guid.NewGuid() });
        var sagaInstance = new SagaInstance<TestOrderSagaData>();
        var ctx = new SagaConsumeContext<TestOrderSagaData, TestOrderCreatedEvent>(
            innerContext, sagaInstance, isNew: true);

        ctx.IsNew.Should().BeTrue();
    }

    [Fact]
    public void SagaConsumeContext_GetSagaInstance_ShouldReturnSameInstance()
    {
        TestConsumeContext<TestOrderCreatedEvent> innerContext = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestOrderCreatedEvent { OrderId = "42", CorrelationId = Guid.NewGuid() });
        var sagaInstance = new SagaInstance<TestOrderSagaData>();
        var ctx = new SagaConsumeContext<TestOrderSagaData, TestOrderCreatedEvent>(
            innerContext, sagaInstance, isNew: false);

        ctx.GetSagaInstance().Should().BeSameAs(sagaInstance);
    }

    [Fact]
    public void SagaConsumeContext_NullInnerContext_ShouldThrow()
    {
        var sagaInstance = new SagaInstance<TestOrderSagaData>();

        Action act = () => new SagaConsumeContext<TestOrderSagaData, TestOrderCreatedEvent>(
            null!, sagaInstance, isNew: true);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SagaConsumeContext_NullInstance_ShouldThrow()
    {
        TestConsumeContext<TestOrderCreatedEvent> innerContext = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestOrderCreatedEvent { OrderId = "42", CorrelationId = Guid.NewGuid() });

        Action act = () => new SagaConsumeContext<TestOrderSagaData, TestOrderCreatedEvent>(
            innerContext, null!, isNew: true);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task RedisSagaRepository_CreateAndFind_ShouldReturnSaga()
    {
        RedisSagaRepository<TestOrderSagaData> repository = CreateRedisRepository();
        var id = Guid.NewGuid();

        SagaInstance<TestOrderSagaData> created = await repository.CreateAsync(id, "Initial");
        SagaInstance<TestOrderSagaData>? retrieved = await repository.FindAsync(id);

        retrieved.Should().NotBeNull();
        retrieved!.CorrelationId.Should().Be(id);
        retrieved.CurrentState.Should().Be("Initial");
        created.Should().NotBeNull();
    }

    [Fact]
    public async Task RedisSagaRepository_FindNonExistent_ShouldReturnNull()
    {
        RedisSagaRepository<TestOrderSagaData> repository = CreateRedisRepository();

        SagaInstance<TestOrderSagaData>? result = await repository.FindAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task RedisSagaRepository_DeleteAsync_ShouldRemoveSaga()
    {
        RedisSagaRepository<TestOrderSagaData> repository = CreateRedisRepository();
        var id = Guid.NewGuid();
        await repository.CreateAsync(id, "Initial");

        bool deleted = await repository.DeleteAsync(id);
        SagaInstance<TestOrderSagaData>? result = await repository.FindAsync(id);

        deleted.Should().BeTrue();
        result.Should().BeNull();
    }

    [Fact]
    public async Task RedisSagaRepository_UpdateAsync_ShouldModifyData()
    {
        RedisSagaRepository<TestOrderSagaData> repository = CreateRedisRepository();
        var id = Guid.NewGuid();
        SagaInstance<TestOrderSagaData> saga = await repository.CreateAsync(id, "Initial");

        bool updated = await repository.UpdateAsync(
            id,
            expectedVersion: saga.Version,
            instance => instance.Data.OrderId = "updated-order");

        SagaInstance<TestOrderSagaData>? result = await repository.FindAsync(id);
        updated.Should().BeTrue();
        result!.Data.OrderId.Should().Be("updated-order");
    }

    [Fact]
    public async Task RedisSagaRepository_CreateDuplicate_ShouldThrow()
    {
        RedisSagaRepository<TestOrderSagaData> repository = CreateRedisRepository();
        var correlationId = Guid.NewGuid();

        await repository.CreateAsync(correlationId, "Initial");

        Func<Task> act = () => repository.CreateAsync(correlationId, "Initial");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task RedisSagaRepository_UpdateAsync_WithWrongVersion_ShouldReturnFalse()
    {
        RedisSagaRepository<TestOrderSagaData> repository = CreateRedisRepository();
        var correlationId = Guid.NewGuid();
        SagaInstance<TestOrderSagaData> saga = await repository.CreateAsync(correlationId, "Initial");

        bool updated = await repository.UpdateAsync(
            correlationId,
            expectedVersion: saga.Version + 10,
            instance => instance.Data.OrderId = "x");

        updated.Should().BeFalse();
    }

    [Fact]
    public async Task RedisSagaRepository_FindByStateAsync_ShouldFilter()
    {
        RedisSagaRepository<TestOrderSagaData> repository = CreateRedisRepository();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        await repository.CreateAsync(id1, "AwaitingPayment");
        await repository.CreateAsync(id2, "Shipped");

        IReadOnlyList<SagaInstance<TestOrderSagaData>> awaiting = await repository.FindByStateAsync("AwaitingPayment");

        awaiting.Should().ContainSingle(s => s.CorrelationId == id1);
    }

    [Fact]
    public async Task RedisSagaRepository_CorruptJson_ShouldReturnNull()
    {
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var repository = new RedisSagaRepository<TestOrderSagaData>(cache);
        var id = Guid.NewGuid();

        await cache.SetStringAsync(RedisSagaKey(id), "{ invalid json");

        SagaInstance<TestOrderSagaData>? result = await repository.FindAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task RedisSagaRepository_Expiration_ShouldRemoveExpiredEntry()
    {
        RedisSagaRepository<TestOrderSagaData> repository = CreateRedisRepository(TimeSpan.FromMilliseconds(50));
        var id = Guid.NewGuid();

        await repository.CreateAsync(id, "Initial");
        await Task.Delay(150);

        SagaInstance<TestOrderSagaData>? result = await repository.FindAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SagaStateMachine_During_WrongState_ShouldNotHandle()
    {
        var machine = new OrderSagaStateMachine();
        var saga = new SagaInstance<TestOrderSagaData>
        {
            CorrelationId = Guid.NewGuid(),
            CurrentState = "Shipped",
            Data = new TestOrderSagaData { OrderId = "42" }
        };
        TestConsumeContext<TestPaymentCompletedEvent> context = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestPaymentCompletedEvent { CorrelationId = saga.CorrelationId });

        bool handled = await machine.ProcessEventAsync(
            saga,
            new TestPaymentCompletedEvent { CorrelationId = saga.CorrelationId },
            context);

        handled.Should().BeFalse();
        saga.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task SagaStateMachine_During_Finalize_ShouldCompleteSaga()
    {
        var machine = new OrderSagaStateMachine();
        var saga = new SagaInstance<TestOrderSagaData>
        {
            CorrelationId = Guid.NewGuid(),
            CurrentState = "AwaitingPayment",
            Data = new TestOrderSagaData { OrderId = "42" }
        };
        TestConsumeContext<TestPaymentCompletedEvent> context = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestPaymentCompletedEvent { CorrelationId = saga.CorrelationId });

        bool handled = await machine.ProcessEventAsync(
            saga,
            new TestPaymentCompletedEvent { CorrelationId = saga.CorrelationId },
            context);

        handled.Should().BeTrue();
        saga.IsCompleted.Should().BeTrue();
        saga.CurrentState.Should().Be("Completed");
    }

    [Fact]
    public async Task SagaStateMachine_During_TransitionTo_ShouldChangeState()
    {
        var machine = new ShipmentSagaStateMachine();
        var saga = new SagaInstance<TestOrderSagaData>
        {
            CorrelationId = Guid.NewGuid(),
            CurrentState = "AwaitingShipment",
            Data = new TestOrderSagaData { OrderId = "99" }
        };
        TestConsumeContext<TestOrderCreatedEvent> context = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestOrderCreatedEvent { CorrelationId = saga.CorrelationId, OrderId = "99" });

        await machine.ProcessEventAsync(
            saga,
            new TestOrderCreatedEvent { CorrelationId = saga.CorrelationId, OrderId = "99" },
            context);

        saga.CurrentState.Should().Be("Shipped");
        saga.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task SagaConsumerProcessor_NewSaga_ShouldCreateAndPersist()
    {
        var repository = new InMemorySagaRepository<TestOrderSagaData>();
        var correlationId = Guid.NewGuid();
        IServiceProvider provider = BuildSagaProcessorProvider(repository, new StartOrderSagaConsumer());

        var processor = new SagaConsumerProcessor<TestOrderSagaData, TestOrderCreatedEvent, StartOrderSagaConsumer>(provider);
        TestConsumeContext<TestOrderCreatedEvent> context = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestOrderCreatedEvent { CorrelationId = correlationId, OrderId = "100" },
            b => b.WithServiceProvider(provider));

        await processor.ProcessAsync(context);

        SagaInstance<TestOrderSagaData>? saga = await repository.FindAsync(correlationId);
        saga.Should().NotBeNull();
        saga!.CurrentState.Should().Be("AwaitingPayment");
        saga.Data.OrderId.Should().Be("100");
    }

    [Fact]
    public async Task SagaConsumerProcessor_ExistingSaga_ShouldUpdateState()
    {
        var repository = new InMemorySagaRepository<TestOrderSagaData>();
        var correlationId = Guid.NewGuid();
        await repository.CreateAsync(correlationId, "AwaitingPayment", new TestOrderSagaData { OrderId = "100" });

        IServiceProvider provider = BuildSagaProcessorProvider(repository, new ContinueOrderSagaConsumer());
        var processor = new SagaConsumerProcessor<TestOrderSagaData, TestPaymentCompletedEvent, ContinueOrderSagaConsumer>(provider);
        TestConsumeContext<TestPaymentCompletedEvent> context = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestPaymentCompletedEvent { CorrelationId = correlationId },
            b => b.WithServiceProvider(provider));

        await processor.ProcessAsync(context);

        SagaInstance<TestOrderSagaData>? saga = await repository.FindAsync(correlationId);
        saga!.Data.Paid.Should().BeTrue();
    }

    [Fact]
    public async Task SagaConsumerProcessor_CompletedSaga_ShouldIgnoreMessage()
    {
        var repository = new InMemorySagaRepository<TestOrderSagaData>();
        var correlationId = Guid.NewGuid();
        SagaInstance<TestOrderSagaData> saga = await repository.CreateAsync(correlationId, "Completed");
        saga.Complete();
        await repository.SaveAsync(saga);

        var consumer = new StartOrderSagaConsumer();
        IServiceProvider provider = BuildSagaProcessorProvider(repository, consumer);
        var processor = new SagaConsumerProcessor<TestOrderSagaData, TestOrderCreatedEvent, StartOrderSagaConsumer>(provider);
        TestConsumeContext<TestOrderCreatedEvent> context = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestOrderCreatedEvent { CorrelationId = correlationId, OrderId = "100" },
            b => b.WithServiceProvider(provider));

        await processor.ProcessAsync(context);

        consumer.ConsumeCount.Should().Be(0);
    }

    [Fact]
    public async Task SagaConsumerProcessor_SagaNotFoundAndCannotStart_ShouldInvokeOnSagaNotFound()
    {
        var repository = new InMemorySagaRepository<TestOrderSagaData>();
        var correlationId = Guid.NewGuid();
        var consumer = new NonStartingOrderSagaConsumer();
        IServiceProvider provider = BuildSagaProcessorProvider(repository, consumer);
        var processor = new SagaConsumerProcessor<TestOrderSagaData, TestOrderCreatedEvent, NonStartingOrderSagaConsumer>(provider);
        TestConsumeContext<TestOrderCreatedEvent> context = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestOrderCreatedEvent { CorrelationId = correlationId, OrderId = "100" },
            b => b.WithServiceProvider(provider));

        await processor.ProcessAsync(context);

        consumer.NotFoundCalled.Should().BeTrue();
        (await repository.FindAsync(correlationId)).Should().BeNull();
    }

    [Fact]
    public async Task SagaConsumerProcessor_ConsumerThrows_ShouldFaultSaga()
    {
        var repository = new InMemorySagaRepository<TestOrderSagaData>();
        var correlationId = Guid.NewGuid();
        await repository.CreateAsync(correlationId, "AwaitingPayment", new TestOrderSagaData { OrderId = "100" });

        IServiceProvider provider = BuildSagaProcessorProvider(repository, new FaultingOrderSagaConsumer());
        var processor = new SagaConsumerProcessor<TestOrderSagaData, TestOrderCreatedEvent, FaultingOrderSagaConsumer>(provider);
        TestConsumeContext<TestOrderCreatedEvent> context = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestOrderCreatedEvent { CorrelationId = correlationId, OrderId = "100" },
            b => b.WithServiceProvider(provider));

        Func<Task> act = () => processor.ProcessAsync(context);

        await act.Should().ThrowAsync<InvalidOperationException>();
        SagaInstance<TestOrderSagaData>? saga = await repository.FindAsync(correlationId);
        saga!.IsFaulted.Should().BeTrue();
    }

    private static IServiceProvider BuildSagaProcessorProvider<TConsumer>(
        ISagaRepository<TestOrderSagaData> repository,
        TConsumer consumer)
        where TConsumer : class
    {
        var services = new ServiceCollection();
        services.AddSingleton(repository);
        services.AddSingleton(consumer);
        return services.BuildServiceProvider();
    }

    private sealed class OrderSagaStateMachine : SagaStateMachine<TestOrderSagaData>
    {
        public OrderSagaStateMachine()
        {
            State("AwaitingPayment");

            Initially(
                When<TestOrderCreatedEvent>()
                    .TransitionTo("AwaitingPayment")
                    .Then(ctx => ctx.Data.OrderId = ctx.Event.OrderId));

            During("AwaitingPayment",
                When<TestPaymentCompletedEvent>()
                    .Finalize());
        }
    }

    private sealed class ShipmentSagaStateMachine : SagaStateMachine<TestOrderSagaData>
    {
        public ShipmentSagaStateMachine()
        {
            State("AwaitingShipment");
            State("Shipped");

            During("AwaitingShipment",
                When<TestOrderCreatedEvent>()
                    .TransitionTo("Shipped")
                    .Then(ctx => ctx.Data.OrderId = ctx.Event.OrderId));
        }
    }

    private sealed class StartOrderSagaConsumer : ISagaConsumer<TestOrderSagaData, TestOrderCreatedEvent>
    {
        public int ConsumeCount { get; private set; }

        public Guid GetCorrelationId(TestOrderCreatedEvent message)
        {
            return message.CorrelationId;
        }

        public bool CanStartSaga(TestOrderCreatedEvent message)
        {
            return true;
        }

        public Task ConsumeAsync(
            ISagaConsumeContext<TestOrderSagaData, TestOrderCreatedEvent> context,
            CancellationToken cancellationToken = default)
        {
            ConsumeCount++;
            context.SagaData.OrderId = context.Message.OrderId;
            return context.TransitionToAsync("AwaitingPayment", cancellationToken);
        }
    }

    private sealed class ContinueOrderSagaConsumer : ISagaConsumer<TestOrderSagaData, TestPaymentCompletedEvent>
    {
        public Guid GetCorrelationId(TestPaymentCompletedEvent message)
        {
            return message.CorrelationId;
        }

        public Task ConsumeAsync(
            ISagaConsumeContext<TestOrderSagaData, TestPaymentCompletedEvent> context,
            CancellationToken cancellationToken = default)
        {
            context.SagaData.Paid = true;
            return Task.CompletedTask;
        }
    }

    private sealed class NonStartingOrderSagaConsumer : ISagaConsumer<TestOrderSagaData, TestOrderCreatedEvent>
    {
        public bool NotFoundCalled { get; private set; }

        public Guid GetCorrelationId(TestOrderCreatedEvent message)
        {
            return message.CorrelationId;
        }

        public Task OnSagaNotFoundAsync(
            IConsumeContext<TestOrderCreatedEvent> context,
            Guid correlationId,
            CancellationToken cancellationToken = default)
        {
            NotFoundCalled = true;
            return Task.CompletedTask;
        }

        public Task ConsumeAsync(
            ISagaConsumeContext<TestOrderSagaData, TestOrderCreatedEvent> context,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FaultingOrderSagaConsumer : ISagaConsumer<TestOrderSagaData, TestOrderCreatedEvent>
    {
        public Guid GetCorrelationId(TestOrderCreatedEvent message)
        {
            return message.CorrelationId;
        }

        public Task ConsumeAsync(
            ISagaConsumeContext<TestOrderSagaData, TestOrderCreatedEvent> context,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("consumer failed");
        }
    }
}
