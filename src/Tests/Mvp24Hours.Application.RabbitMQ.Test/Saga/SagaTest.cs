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
