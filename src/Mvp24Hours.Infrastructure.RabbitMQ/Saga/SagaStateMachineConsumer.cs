//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Contract;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Saga
{
    /// <summary>
    /// Generic consumer that processes messages through a saga state machine.
    /// </summary>
    /// <typeparam name="TData">The type of saga data.</typeparam>
    /// <typeparam name="TMessage">The type of message.</typeparam>
    /// <typeparam name="TMachine">The type of saga state machine.</typeparam>
    public class SagaStateMachineConsumer<TData, TMessage, TMachine> : IMessageConsumer<TMessage>
        where TData : class, new()
        where TMessage : class
        where TMachine : SagaStateMachine<TData>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SagaStateMachineConsumer<TData, TMessage, TMachine>>? _logger;

        /// <summary>
        /// Creates a new saga state machine consumer.
        /// </summary>
        public SagaStateMachineConsumer(
            IServiceProvider serviceProvider,
            ILogger<SagaStateMachineConsumer<TData, TMessage, TMachine>>? logger = null)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task ConsumeAsync(IConsumeContext<TMessage> context, CancellationToken cancellationToken = default)
        {
            using var scope = context.CreateScope();

            var machine = scope.ServiceProvider.GetRequiredService<TMachine>();
            var repository = scope.ServiceProvider.GetRequiredService<ISagaRepository<TData>>();

            // Extract correlation ID
            Guid correlationId;
            try
            {
                correlationId = machine.GetCorrelationId(context.Message);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex,
                    "Failed to extract correlation ID from message {MessageType}",
                    typeof(TMessage).Name);
                throw;
            }

            _logger?.LogDebug(
                "Processing message {MessageType} with correlation ID {CorrelationId} through state machine {Machine}",
                typeof(TMessage).Name, correlationId, typeof(TMachine).Name);

            // Find or create saga instance
            var instance = await repository.FindAsync(correlationId, cancellationToken);
            var isNew = false;

            if (instance == null)
            {
                if (machine.CanEventStartSaga<TMessage>())
                {
                    instance = await repository.CreateAsync(
                        correlationId,
                        machine.InitialState,
                        new TData(),
                        cancellationToken);
                    isNew = true;

                    _logger?.LogInformation(
                        "Created new saga instance {SagaId} from message {MessageType} via state machine",
                        correlationId, typeof(TMessage).Name);
                }
                else
                {
                    _logger?.LogWarning(
                        "Saga instance not found for correlation ID {CorrelationId} and event {MessageType} cannot start saga in state machine {Machine}",
                        correlationId, typeof(TMessage).Name, typeof(TMachine).Name);
                    return;
                }
            }

            // Check if saga can still process messages
            if (instance.IsCompleted || instance.IsFaulted)
            {
                _logger?.LogWarning(
                    "Saga {SagaId} is {Status}, ignoring message {MessageType}",
                    correlationId,
                    instance.IsCompleted ? "completed" : "faulted",
                    typeof(TMessage).Name);
                return;
            }

            try
            {
                var previousState = instance.CurrentState;

                // Process through state machine
                var handled = await machine.ProcessEventAsync(instance, context.Message, context, cancellationToken);

                if (!handled)
                {
                    _logger?.LogWarning(
                        "No handler matched for message {MessageType} in state {State} for saga {SagaId}",
                        typeof(TMessage).Name, previousState, correlationId);
                    return;
                }

                // Save saga state
                await repository.SaveAsync(instance, cancellationToken);

                _logger?.LogDebug(
                    "Saga {SagaId} transitioned from {PreviousState} to {NewState} via {MessageType}",
                    correlationId, previousState, instance.CurrentState, typeof(TMessage).Name);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex,
                    "Error processing message {MessageType} through state machine for saga {SagaId}",
                    typeof(TMessage).Name, correlationId);

                instance.Fault(ex.Message);
                await repository.SaveAsync(instance, cancellationToken);

                throw;
            }
        }
    }
}

