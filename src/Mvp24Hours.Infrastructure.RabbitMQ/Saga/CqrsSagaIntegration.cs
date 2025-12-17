//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Cqrs.Saga;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Contract;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Saga
{
    /// <summary>
    /// Adapter that bridges CQRS ISaga with RabbitMQ events.
    /// Enables CQRS sagas to be driven by RabbitMQ messages.
    /// </summary>
    /// <typeparam name="TData">The type of saga data.</typeparam>
    /// <typeparam name="TSaga">The type of CQRS saga.</typeparam>
    /// <remarks>
    /// <para>
    /// This adapter allows existing CQRS sagas to receive events from RabbitMQ.
    /// It maps RabbitMQ messages to domain events that the saga can handle.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register the adapter
    /// services.AddCqrsSagaRabbitMQIntegration&lt;OrderSagaData, OrderSaga&gt;();
    /// 
    /// // The saga will now receive RabbitMQ messages as domain events
    /// </code>
    /// </example>
    public class CqrsSagaAdapter<TData, TSaga>
        where TData : class
        where TSaga : SagaBase<TData>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ISagaOrchestrator _orchestrator;
        private readonly ILogger<CqrsSagaAdapter<TData, TSaga>>? _logger;

        /// <summary>
        /// Creates a new CQRS saga adapter.
        /// </summary>
        public CqrsSagaAdapter(
            IServiceProvider serviceProvider,
            ISagaOrchestrator orchestrator,
            ILogger<CqrsSagaAdapter<TData, TSaga>>? logger = null)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _logger = logger;
        }

        /// <summary>
        /// Starts a new CQRS saga from a RabbitMQ message.
        /// </summary>
        /// <typeparam name="TMessage">The type of message.</typeparam>
        /// <param name="context">The consume context.</param>
        /// <param name="dataFactory">Factory to create saga data from the message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The saga result.</returns>
        public async Task<SagaResult<TData>> StartSagaAsync<TMessage>(
            IConsumeContext<TMessage> context,
            Func<TMessage, TData> dataFactory,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(dataFactory);

            var data = dataFactory(context.Message);

            TelemetryHelper.Execute(
                TelemetryLevels.Information,
                "cqrs-saga-start-from-rabbitmq",
                $"saga:{typeof(TSaga).Name}|message:{typeof(TMessage).Name}");

            _logger?.LogInformation(
                "Starting CQRS saga {SagaType} from RabbitMQ message {MessageType}",
                typeof(TSaga).Name, typeof(TMessage).Name);

            var result = await _orchestrator.ExecuteAsync<TSaga, TData>(data, null, cancellationToken);

            if (result.IsSuccess)
            {
                _logger?.LogInformation(
                    "CQRS saga {SagaId} completed successfully via RabbitMQ",
                    result.SagaId);
            }
            else
            {
                _logger?.LogWarning(
                    "CQRS saga {SagaId} failed via RabbitMQ: {Error}",
                    result.SagaId, result.ErrorMessage);
            }

            return result;
        }

        /// <summary>
        /// Resumes a CQRS saga from a saved state triggered by a RabbitMQ message.
        /// </summary>
        /// <param name="sagaId">The saga ID to resume.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The saga result.</returns>
        public async Task<SagaResult<TData>?> ResumeSagaAsync(
            Guid sagaId,
            CancellationToken cancellationToken = default)
        {
            var stateStore = _serviceProvider.GetService<ISagaStateStore>();
            if (stateStore == null)
            {
                _logger?.LogWarning("ISagaStateStore not registered, cannot resume saga {SagaId}", sagaId);
                return null;
            }

            var state = await stateStore.GetAsync<TData>(sagaId, cancellationToken);
            if (state == null)
            {
                _logger?.LogWarning("Saga state not found for {SagaId}", sagaId);
                return null;
            }

            TelemetryHelper.Execute(
                TelemetryLevels.Information,
                "cqrs-saga-resume-from-rabbitmq",
                $"sagaId:{sagaId}|type:{typeof(TSaga).Name}");

            _logger?.LogInformation(
                "Resuming CQRS saga {SagaId} of type {SagaType} from RabbitMQ event",
                sagaId, typeof(TSaga).Name);

            return await _orchestrator.ResumeAsync<TSaga, TData>(sagaId, cancellationToken);
        }
    }

    /// <summary>
    /// Message consumer that drives a CQRS saga from RabbitMQ events.
    /// </summary>
    /// <typeparam name="TData">The type of saga data.</typeparam>
    /// <typeparam name="TSaga">The type of CQRS saga.</typeparam>
    /// <typeparam name="TMessage">The type of message.</typeparam>
    public class CqrsSagaMessageConsumer<TData, TSaga, TMessage> : IMessageConsumer<TMessage>
        where TData : class
        where TSaga : SagaBase<TData>
        where TMessage : class
    {
        private readonly CqrsSagaAdapter<TData, TSaga> _adapter;
        private readonly Func<TMessage, TData> _dataFactory;
        private readonly Func<TMessage, Guid>? _correlationIdExtractor;
        private readonly ILogger<CqrsSagaMessageConsumer<TData, TSaga, TMessage>>? _logger;

        /// <summary>
        /// Creates a new CQRS saga message consumer.
        /// </summary>
        public CqrsSagaMessageConsumer(
            CqrsSagaAdapter<TData, TSaga> adapter,
            Func<TMessage, TData> dataFactory,
            Func<TMessage, Guid>? correlationIdExtractor = null,
            ILogger<CqrsSagaMessageConsumer<TData, TSaga, TMessage>>? logger = null)
        {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _dataFactory = dataFactory ?? throw new ArgumentNullException(nameof(dataFactory));
            _correlationIdExtractor = correlationIdExtractor;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task ConsumeAsync(IConsumeContext<TMessage> context, CancellationToken cancellationToken = default)
        {
            // Check if this should resume an existing saga
            if (_correlationIdExtractor != null)
            {
                var correlationId = _correlationIdExtractor(context.Message);
                var result = await _adapter.ResumeSagaAsync(correlationId, cancellationToken);
                if (result != null)
                {
                    _logger?.LogDebug(
                        "Resumed existing CQRS saga {SagaId} from message {MessageType}",
                        correlationId, typeof(TMessage).Name);
                    return;
                }
            }

            // Start a new saga
            await _adapter.StartSagaAsync(context, _dataFactory, cancellationToken);
        }
    }

    /// <summary>
    /// Factory for creating CQRS saga message consumers.
    /// </summary>
    public static class CqrsSagaConsumerFactory
    {
        /// <summary>
        /// Creates a CQRS saga message consumer.
        /// </summary>
        /// <typeparam name="TData">The type of saga data.</typeparam>
        /// <typeparam name="TSaga">The type of CQRS saga.</typeparam>
        /// <typeparam name="TMessage">The type of message.</typeparam>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="dataFactory">Factory to create saga data from the message.</param>
        /// <param name="correlationIdExtractor">Optional correlation ID extractor for resuming sagas.</param>
        /// <returns>The message consumer.</returns>
        public static IMessageConsumer<TMessage> Create<TData, TSaga, TMessage>(
            IServiceProvider serviceProvider,
            Func<TMessage, TData> dataFactory,
            Func<TMessage, Guid>? correlationIdExtractor = null)
            where TData : class
            where TSaga : SagaBase<TData>
            where TMessage : class
        {
            var adapter = serviceProvider.GetRequiredService<CqrsSagaAdapter<TData, TSaga>>();
            var logger = serviceProvider.GetService<ILogger<CqrsSagaMessageConsumer<TData, TSaga, TMessage>>>();

            return new CqrsSagaMessageConsumer<TData, TSaga, TMessage>(
                adapter, dataFactory, correlationIdExtractor, logger);
        }
    }
}

