using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Persistence;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing;

namespace Mvp24Hours.Application.RabbitMQ.Test.Saga;

public class SagaTest
{
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
        Guid correlationId = Guid.NewGuid();

        await repository.CreateAsync(correlationId, "Initial");

        Func<Task> act = () => repository.CreateAsync(correlationId, "Initial");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task InMemorySagaRepository_UpdateAsync_WithWrongVersion_ShouldReturnFalse()
    {
        var repository = new InMemorySagaRepository<TestOrderSagaData>();
        Guid correlationId = Guid.NewGuid();
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
        Guid id1 = Guid.NewGuid();
        Guid id2 = Guid.NewGuid();
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
        Guid id = Guid.NewGuid();

        var created = await repository.CreateAsync(id, "Initial");
        var retrieved = await repository.FindAsync(id);

        retrieved.Should().NotBeNull();
        retrieved!.CorrelationId.Should().Be(id);
        retrieved.CurrentState.Should().Be("Initial");
        created.Should().NotBeNull();
    }

    [Fact]
    public async Task InMemorySagaRepository_FindNonExistent_ShouldReturnNull()
    {
        var repository = new InMemorySagaRepository<TestOrderSagaData>();

        var result = await repository.FindAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task InMemorySagaRepository_DeleteAsync_ShouldRemoveSaga()
    {
        var repository = new InMemorySagaRepository<TestOrderSagaData>();
        Guid id = Guid.NewGuid();
        await repository.CreateAsync(id, "Initial");

        bool deleted = await repository.DeleteAsync(id);
        var result = await repository.FindAsync(id);

        deleted.Should().BeTrue();
        result.Should().BeNull();
    }

    [Fact]
    public async Task InMemorySagaRepository_UpdateAsync_ShouldModifyData()
    {
        var repository = new InMemorySagaRepository<TestOrderSagaData>();
        Guid id = Guid.NewGuid();
        SagaInstance<TestOrderSagaData> saga = await repository.CreateAsync(id, "Initial");

        bool updated = await repository.UpdateAsync(
            id,
            expectedVersion: saga.Version,
            instance => instance.Data.OrderId = "updated-order");

        var result = await repository.FindAsync(id);
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
        var innerContext = RabbitMQTestHelpers.CreateTestConsumeContext(
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
        var innerContext = RabbitMQTestHelpers.CreateTestConsumeContext(
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
        var innerContext = RabbitMQTestHelpers.CreateTestConsumeContext(
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
        var innerContext = RabbitMQTestHelpers.CreateTestConsumeContext(
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
        var innerContext = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestOrderCreatedEvent { OrderId = "42", CorrelationId = Guid.NewGuid() });
        var sagaInstance = new SagaInstance<TestOrderSagaData>();
        var ctx = new SagaConsumeContext<TestOrderSagaData, TestOrderCreatedEvent>(
            innerContext, sagaInstance, isNew: true);

        ctx.IsNew.Should().BeTrue();
    }

    [Fact]
    public void SagaConsumeContext_GetSagaInstance_ShouldReturnSameInstance()
    {
        var innerContext = RabbitMQTestHelpers.CreateTestConsumeContext(
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
        var innerContext = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestOrderCreatedEvent { OrderId = "42", CorrelationId = Guid.NewGuid() });

        Action act = () => new SagaConsumeContext<TestOrderSagaData, TestOrderCreatedEvent>(
            innerContext, null!, isNew: true);

        act.Should().Throw<ArgumentNullException>();
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
}
