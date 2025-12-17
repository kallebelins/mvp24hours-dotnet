//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Contract;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Saga
{
    /// <summary>
    /// Processes messages for saga consumers with automatic correlation and state management.
    /// </summary>
    /// <typeparam name="TData">The type of saga data.</typeparam>
    /// <typeparam name="TMessage">The type of message.</typeparam>
    /// <typeparam name="TConsumer">The type of saga consumer.</typeparam>
    public class SagaConsumerProcessor<TData, TMessage, TConsumer>
        where TData : class, new()
        where TMessage : class
        where TConsumer : class, ISagaConsumer<TData, TMessage>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SagaConsumerProcessor<TData, TMessage, TConsumer>>? _logger;

        /// <summary>
        /// Creates a new saga consumer processor.
        /// </summary>
        public SagaConsumerProcessor(
            IServiceProvider serviceProvider,
            ILogger<SagaConsumerProcessor<TData, TMessage, TConsumer>>? logger = null)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger;
        }

        /// <summary>
        /// Processes a message by finding/creating a saga instance and invoking the consumer.
        /// </summary>
        /// <param name="context">The consume context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task ProcessAsync(IConsumeContext<TMessage> context, CancellationToken cancellationToken = default)
        {
            using var scope = context.CreateScope();
            
            var consumer = scope.ServiceProvider.GetRequiredService<TConsumer>();
            var repository = scope.ServiceProvider.GetRequiredService<ISagaRepository<TData>>();
            var scheduler = scope.ServiceProvider.GetService<IMessageScheduler>();
            var rabbitClient = scope.ServiceProvider.GetService<IMvpRabbitMQClient>();

            // Extract correlation ID from message
            var correlationId = consumer.GetCorrelationId(context.Message);

            TelemetryHelper.Execute(
                TelemetryLevels.Verbose,
                "saga-consumer-process",
                $"type:{typeof(TMessage).Name}|correlationId:{correlationId}|consumer:{typeof(TConsumer).Name}");

            _logger?.LogDebug(
                "Processing message {MessageType} with correlation ID {CorrelationId}",
                typeof(TMessage).Name, correlationId);

            // Find or create saga instance
            var instance = await repository.FindAsync(correlationId, cancellationToken);
            var isNew = false;

            if (instance == null)
            {
                if (consumer.CanStartSaga(context.Message))
                {
                    instance = await repository.CreateAsync(
                        correlationId,
                        "Initial",
                        new TData(),
                        cancellationToken);
                    isNew = true;

                    _logger?.LogInformation(
                        "Created new saga instance {SagaId} from message {MessageType}",
                        correlationId, typeof(TMessage).Name);

                    TelemetryHelper.Execute(
                        TelemetryLevels.Information,
                        "saga-instance-created",
                        $"sagaId:{correlationId}|type:{typeof(TData).Name}");
                }
                else
                {
                    _logger?.LogWarning(
                        "Saga instance not found for correlation ID {CorrelationId} and message {MessageType} cannot start saga",
                        correlationId, typeof(TMessage).Name);

                    await consumer.OnSagaNotFoundAsync(context, correlationId, cancellationToken);
                    return;
                }
            }

            // Check if saga can still process messages
            if (instance.IsCompleted)
            {
                _logger?.LogWarning(
                    "Saga {SagaId} is already completed, ignoring message {MessageType}",
                    correlationId, typeof(TMessage).Name);
                return;
            }

            if (instance.IsFaulted)
            {
                _logger?.LogWarning(
                    "Saga {SagaId} is faulted, ignoring message {MessageType}",
                    correlationId, typeof(TMessage).Name);
                return;
            }

            // Create saga context and invoke consumer
            var sagaContext = new SagaConsumeContext<TData, TMessage>(
                context,
                instance,
                isNew,
                scheduler,
                rabbitClient,
                repository);

            try
            {
                await consumer.ConsumeAsync(sagaContext, cancellationToken);

                // Save saga state
                await repository.SaveAsync(instance, cancellationToken);

                _logger?.LogDebug(
                    "Saga {SagaId} processed message {MessageType}, new state: {State}",
                    correlationId, typeof(TMessage).Name, instance.CurrentState);

                TelemetryHelper.Execute(
                    TelemetryLevels.Information,
                    "saga-message-processed",
                    $"sagaId:{correlationId}|message:{typeof(TMessage).Name}|state:{instance.CurrentState}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex,
                    "Error processing message {MessageType} for saga {SagaId}",
                    typeof(TMessage).Name, correlationId);

                instance.Fault(ex.Message);
                await repository.SaveAsync(instance, cancellationToken);

                TelemetryHelper.Execute(
                    TelemetryLevels.Error,
                    "saga-message-failed",
                    $"sagaId:{correlationId}|message:{typeof(TMessage).Name}|error:{ex.Message}");

                throw;
            }
        }
    }

    /// <summary>
    /// Message consumer adapter that wraps a saga consumer.
    /// </summary>
    /// <typeparam name="TData">The type of saga data.</typeparam>
    /// <typeparam name="TMessage">The type of message.</typeparam>
    /// <typeparam name="TConsumer">The type of saga consumer.</typeparam>
    public class SagaMessageConsumerAdapter<TData, TMessage, TConsumer> : IMessageConsumer<TMessage>
        where TData : class, new()
        where TMessage : class
        where TConsumer : class, ISagaConsumer<TData, TMessage>
    {
        private readonly SagaConsumerProcessor<TData, TMessage, TConsumer> _processor;

        /// <summary>
        /// Creates a new saga message consumer adapter.
        /// </summary>
        public SagaMessageConsumerAdapter(IServiceProvider serviceProvider)
        {
            _processor = new SagaConsumerProcessor<TData, TMessage, TConsumer>(
                serviceProvider,
                serviceProvider.GetService<ILogger<SagaConsumerProcessor<TData, TMessage, TConsumer>>>());
        }

        /// <inheritdoc />
        public Task ConsumeAsync(IConsumeContext<TMessage> context, CancellationToken cancellationToken = default)
        {
            return _processor.ProcessAsync(context, cancellationToken);
        }
    }
}

