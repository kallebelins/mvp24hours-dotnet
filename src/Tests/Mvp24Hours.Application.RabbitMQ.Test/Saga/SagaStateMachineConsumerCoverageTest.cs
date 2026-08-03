using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Persistence;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing;

namespace Mvp24Hours.Application.RabbitMQ.Test.Saga;

[Trait("Category", "Unit")]
public class SagaStateMachineConsumerCoverageTest
{
    [Fact]
    public void CanEventStartSaga_WithInitialHandler_ShouldReturnTrue()
    {
        var machine = new StartableOrderStateMachine();

        machine.CanEventStartSaga<TestOrderCreatedEvent>().Should().BeTrue();
        machine.CanEventStartSaga<TestPaymentCompletedEvent>().Should().BeFalse();
    }

    [Fact]
    public void GetCorrelationId_WithSagaIdProperty_ShouldExtractGuid()
    {
        var machine = new StartableOrderStateMachine();
        var correlationId = Guid.NewGuid();
        var message = new MessageWithSagaId { SagaId = correlationId };

        machine.GetCorrelationId(message).Should().Be(correlationId);
    }

    [Fact]
    public void GetCorrelationId_WithIdProperty_ShouldExtractGuid()
    {
        var machine = new StartableOrderStateMachine();
        var correlationId = Guid.NewGuid();
        var message = new MessageWithId { Id = correlationId };

        machine.GetCorrelationId(message).Should().Be(correlationId);
    }

    [Fact]
    public void GetCorrelationId_WithoutKnownProperty_ShouldThrow()
    {
        var machine = new StartableOrderStateMachine();

        Action act = () => machine.GetCorrelationId(new MessageWithoutCorrelation());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task SagaStateMachine_WithIfConditionFalse_ShouldSkipTransition()
    {
        var machine = new ConditionalOrderStateMachine();
        var saga = new SagaInstance<TestOrderSagaData>
        {
            CorrelationId = Guid.NewGuid(),
            CurrentState = "Initial",
            Data = new TestOrderSagaData { OrderId = "skip" }
        };
        TestConsumeContext<TestOrderCreatedEvent> context = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestOrderCreatedEvent { CorrelationId = saga.CorrelationId, OrderId = "skip" });

        bool handled = await machine.ProcessEventAsync(
            saga,
            new TestOrderCreatedEvent { CorrelationId = saga.CorrelationId, OrderId = "skip" },
            context);

        handled.Should().BeTrue();
        saga.CurrentState.Should().Be("Initial");
    }

    [Fact]
    public async Task SagaStateMachine_SetCompletedWhenEnter_ShouldInvokeOnCompleted()
    {
        var machine = new ShippedCompletionStateMachine();
        bool completedCalled = false;
        machine.SetCompletedCallback(_ => completedCalled = true);

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

        completedCalled.Should().BeTrue();
        saga.IsCompleted.Should().BeTrue();
        saga.CurrentState.Should().Be("Completed");
    }

    [Fact]
    public async Task SagaStateMachine_OnFaultedCallback_ShouldInvokeWhenHandlerThrows()
    {
        var machine = new FaultingOrderStateMachine();
        Exception? captured = null;
        machine.SetFaultedCallback((_, ex) => captured = ex);

        var saga = new SagaInstance<TestOrderSagaData>
        {
            CorrelationId = Guid.NewGuid(),
            CurrentState = "Initial"
        };
        TestConsumeContext<TestOrderCreatedEvent> context = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestOrderCreatedEvent { CorrelationId = saga.CorrelationId, OrderId = "42" });

        Func<Task> act = () => machine.ProcessEventAsync(
            saga,
            new TestOrderCreatedEvent { CorrelationId = saga.CorrelationId, OrderId = "42" },
            context);

        await act.Should().ThrowAsync<InvalidOperationException>();
        captured.Should().NotBeNull();
        saga.IsFaulted.Should().BeTrue();
    }

    [Fact]
    public async Task SagaStateMachineConsumer_NewSaga_ShouldCreateAndPersist()
    {
        var repository = new InMemorySagaRepository<TestOrderSagaData>();
        var correlationId = Guid.NewGuid();
        IServiceProvider provider = BuildStateMachineProvider(repository);

        var consumer = new SagaStateMachineConsumer<TestOrderSagaData, TestOrderCreatedEvent, StartableOrderStateMachine>(provider);
        TestConsumeContext<TestOrderCreatedEvent> context = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestOrderCreatedEvent { CorrelationId = correlationId, OrderId = "100" },
            b => b.WithServiceProvider(provider));

        await consumer.ConsumeAsync(context);

        SagaInstance<TestOrderSagaData>? saga = await repository.FindAsync(correlationId);
        saga.Should().NotBeNull();
        saga!.CurrentState.Should().Be("AwaitingPayment");
    }

    [Fact]
    public async Task SagaStateMachineConsumer_CompletedSaga_ShouldIgnoreMessage()
    {
        var repository = new InMemorySagaRepository<TestOrderSagaData>();
        var correlationId = Guid.NewGuid();
        SagaInstance<TestOrderSagaData> saga = await repository.CreateAsync(correlationId, "Completed");
        saga.Complete();
        await repository.SaveAsync(saga);

        IServiceProvider provider = BuildStateMachineProvider(repository);
        var consumer = new SagaStateMachineConsumer<TestOrderSagaData, TestOrderCreatedEvent, StartableOrderStateMachine>(provider);
        TestConsumeContext<TestOrderCreatedEvent> context = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestOrderCreatedEvent { CorrelationId = correlationId, OrderId = "100" },
            b => b.WithServiceProvider(provider));

        await consumer.ConsumeAsync(context);

        SagaInstance<TestOrderSagaData>? updated = await repository.FindAsync(correlationId);
        updated!.CurrentState.Should().Be("Completed");
    }

    [Fact]
    public async Task SagaStateMachineConsumer_WhenEventCannotStartSaga_ShouldReturnWithoutCreating()
    {
        var repository = new InMemorySagaRepository<TestOrderSagaData>();
        var correlationId = Guid.NewGuid();
        IServiceProvider provider = BuildStateMachineProvider(repository);

        var consumer = new SagaStateMachineConsumer<TestOrderSagaData, TestPaymentCompletedEvent, StartableOrderStateMachine>(provider);
        TestConsumeContext<TestPaymentCompletedEvent> context = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestPaymentCompletedEvent { CorrelationId = correlationId },
            b => b.WithServiceProvider(provider));

        await consumer.ConsumeAsync(context);

        (await repository.FindAsync(correlationId)).Should().BeNull();
    }

    [Fact]
    public async Task SagaMessageConsumerAdapter_ShouldDelegateToProcessor()
    {
        var repository = new InMemorySagaRepository<TestOrderSagaData>();
        var correlationId = Guid.NewGuid();
        IServiceProvider provider = BuildSagaConsumerProvider(repository, new RecordingSagaConsumer());

        var adapter = new SagaMessageConsumerAdapter<TestOrderSagaData, TestOrderCreatedEvent, RecordingSagaConsumer>(provider);
        TestConsumeContext<TestOrderCreatedEvent> context = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestOrderCreatedEvent { CorrelationId = correlationId, OrderId = "adapter" },
            b => b.WithServiceProvider(provider));

        await adapter.ConsumeAsync(context);

        (await repository.FindAsync(correlationId)).Should().NotBeNull();
    }

    [Fact]
    public void SagaConsumerDefinition_GetQueueName_ShouldUseDefaultWhenNotConfigured()
    {
        var definition = new TestSagaConsumerDefinition();
        string sagaName = typeof(TestOrderSagaData).Name.Replace("Data", "").Replace("SagaData", "Saga");
        string expected = $"saga.{sagaName}.{typeof(TestOrderCreatedEvent).Name}".ToLowerInvariant();

        definition.GetQueueName().Should().Be(expected);
    }

    [Fact]
    public void SagaStateMachineConsumerDefinition_GetQueueName_ShouldUseDefaultWhenNotConfigured()
    {
        var definition = new TestStateMachineConsumerDefinition();
        string machineName = typeof(StartableOrderStateMachine).Name.Replace("StateMachine", "").Replace("Saga", "");
        string expected = $"saga.{machineName}.{typeof(TestOrderCreatedEvent).Name}".ToLowerInvariant();

        definition.GetQueueName().Should().Be(expected);
    }

    private static IServiceProvider BuildStateMachineProvider(ISagaRepository<TestOrderSagaData> repository)
    {
        var services = new ServiceCollection();
        services.AddSingleton(repository);
        services.AddSingleton<StartableOrderStateMachine>();
        services.AddSingleton<FaultingOrderStateMachine>();
        services.AddSingleton<ConditionalOrderStateMachine>();
        services.AddSingleton<ShippedCompletionStateMachine>();
        return services.BuildServiceProvider();
    }

    private static IServiceProvider BuildSagaConsumerProvider<TConsumer>(
        ISagaRepository<TestOrderSagaData> repository,
        TConsumer consumer)
        where TConsumer : class
    {
        var services = new ServiceCollection();
        services.AddSingleton(repository);
        services.AddSingleton(consumer);
        return services.BuildServiceProvider();
    }

    private sealed class StartableOrderStateMachine : SagaStateMachine<TestOrderSagaData>
    {
        public StartableOrderStateMachine()
        {
            State("AwaitingPayment");

            Initially(
                When<TestOrderCreatedEvent>()
                    .TransitionTo("AwaitingPayment")
                    .Then(ctx => ctx.Data.OrderId = ctx.Event.OrderId));
        }
    }

    private sealed class ConditionalOrderStateMachine : SagaStateMachine<TestOrderSagaData>
    {
        public ConditionalOrderStateMachine()
        {
            Initially(
                When<TestOrderCreatedEvent>()
                    .If((_, evt) => evt.OrderId != "skip")
                    .TransitionTo("AwaitingPayment"));
        }
    }

    private sealed class ShippedCompletionStateMachine : SagaStateMachine<TestOrderSagaData>
    {
        public ShippedCompletionStateMachine()
        {
            State("AwaitingPayment");
            State("Shipped");

            During("AwaitingPayment",
                When<TestPaymentCompletedEvent>().TransitionTo("Shipped"));

            SetCompletedWhenEnter("Shipped");
        }

        public void SetCompletedCallback(Action<SagaInstance<TestOrderSagaData>> callback)
        {
            OnCompleted(callback);
        }
    }

    private sealed class FaultingOrderStateMachine : SagaStateMachine<TestOrderSagaData>
    {
        public FaultingOrderStateMachine()
        {
            Initially(
                When<TestOrderCreatedEvent>()
                    .ThenAsync(_ => throw new InvalidOperationException("handler failed")));
        }

        public void SetFaultedCallback(Action<SagaInstance<TestOrderSagaData>, Exception> callback)
        {
            OnFaulted(callback);
        }
    }

    private sealed class RecordingSagaConsumer : ISagaConsumer<TestOrderSagaData, TestOrderCreatedEvent>
    {
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
            context.SagaData.OrderId = context.Message.OrderId;
            return context.TransitionToAsync("AwaitingPayment", cancellationToken);
        }
    }

    private sealed class TestSagaConsumerDefinition : SagaConsumerDefinition<TestOrderSagaData, TestOrderCreatedEvent, RecordingSagaConsumer>;

    private sealed class TestStateMachineConsumerDefinition
        : SagaStateMachineConsumerDefinition<TestOrderSagaData, TestOrderCreatedEvent, StartableOrderStateMachine>;

    private sealed class MessageWithSagaId
    {
        public Guid SagaId { get; init; }
    }

    private sealed class MessageWithId
    {
        public Guid Id { get; init; }
    }

    private sealed class MessageWithoutCorrelation;
}
