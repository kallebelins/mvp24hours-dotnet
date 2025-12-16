using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ.Metrics;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ
{
    public class MvpRabbitMQClient : IMvpRabbitMQClient
    {
        #region [ Properties / Fields ]
        private readonly RabbitMQClientOptions _options;
        private readonly IMvpRabbitMQConnection _connection;
        private readonly Dictionary<string, IModel> _channels;
        private readonly IServiceProvider _provider;
        private readonly List<Type> _consumers;
        private readonly IRabbitMQMetrics? _metrics;
        private readonly IRabbitMQStructuredLogger? _structuredLogger;
        private readonly IMessageDeduplicationStore? _deduplicationStore;

        protected virtual RabbitMQClientOptions Options => _options;
        protected virtual IMvpRabbitMQConnection Connection => _connection;
        protected virtual Dictionary<string, IModel> Channels => _channels;
        #endregion

        #region [ Ctors ]
        public MvpRabbitMQClient(IServiceProvider _provider)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-client-ctor");
            this._provider = _provider;
            this._options = _provider.GetService<IOptions<RabbitMQClientOptions>>()?.Value
                ?? _provider.GetService<RabbitMQClientOptions>()
                ?? throw new ArgumentNullException(nameof(_provider));
            this._connection = _provider.GetService<IMvpRabbitMQConnection>()
                ?? throw new ArgumentNullException(nameof(_provider));
            this._channels = [];
            this._consumers = [];
            
            // Optional services
            if (_options.EnableMetrics)
            {
                this._metrics = _provider.GetService<IRabbitMQMetrics>();
            }
            if (_options.EnableStructuredLogging)
            {
                this._structuredLogger = _provider.GetService<IRabbitMQStructuredLogger>();
            }
            if (_options.Deduplication.Enabled)
            {
                this._deduplicationStore = _provider.GetService<IMessageDeduplicationStore>();
            }
        }
        #endregion

        #region [ Methods]
        public virtual string Publish(object message, string routingKey, string? tokenDefault = null)
        {
            return PublishInternal(message, routingKey, tokenDefault, null, null, null);
        }

        public virtual string Publish(object message, string routingKey, byte priority, string? tokenDefault = null)
        {
            return PublishInternal(message, routingKey, tokenDefault, priority, null, null);
        }

        public virtual string Publish(object message, string routingKey, IDictionary<string, object> headers, string? tokenDefault = null)
        {
            return PublishInternal(message, routingKey, tokenDefault, null, headers, null);
        }

        public virtual string PublishWithTtl(object message, string routingKey, int ttlMilliseconds, string? tokenDefault = null)
        {
            return PublishInternal(message, routingKey, tokenDefault, null, null, ttlMilliseconds);
        }

        public virtual Task<string> PublishAsync(object message, string routingKey, string? tokenDefault = null, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => Publish(message, routingKey, tokenDefault), cancellationToken);
        }

        public virtual IEnumerable<string> PublishBatch(IEnumerable<(object Message, string RoutingKey)> messages)
        {
            var results = new List<string>();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-client-publish-batch-start");

                if (!Connection.IsConnected)
                {
                    Connection.TryConnect();
                }

                using var channel = Connection.CreateModel();
                _metrics?.IncrementChannelCreations();
                
                ExchangeDeclare(channel, Options);
                channel.ConfirmSelect();

                var batch = channel.CreateBasicPublishBatch();
                var messageIds = new List<string>();

                foreach (var (msg, rk) in messages)
                {
                    var tokenDefault = Guid.NewGuid().ToString();
                    messageIds.Add(tokenDefault);

                    var bsEvent = msg.ToBusinessEvent(tokenDefault: tokenDefault);
                    var body = Encoding.UTF8.GetBytes(bsEvent.ToSerialize(JsonHelper.JsonBusinessEventSettings()));

                    var properties = channel.CreateBasicProperties();
                    properties.DeliveryMode = 2;
                    properties.ContentType = "application/json";
                    properties.CorrelationId = tokenDefault;
                    properties.MessageId = tokenDefault;
                    SetRedeliveredCount(properties, 0);

                    batch.Add(
                        exchange: Options.Exchange,
                        routingKey: rk ?? Options.RoutingKey ?? string.Empty,
                        mandatory: false,
                        properties: properties,
                        body: new ReadOnlyMemory<byte>(body));
                }

                batch.Publish();
                channel.WaitForConfirmsOrDie(TimeSpan.FromMilliseconds(Options.PublisherConfirm.TimeoutMilliseconds));

                foreach (var msgId in messageIds)
                {
                    _metrics?.IncrementMessagesSent(Options.Exchange);
                    _metrics?.IncrementPublisherConfirms();
                    results.Add(msgId);
                }

                stopwatch.Stop();
                TelemetryHelper.Execute(TelemetryLevels.Information, "rabbitmq-client-publish-batch-success", $"count:{results.Count}|elapsed:{stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                _metrics?.IncrementError(ex.GetType().Name);
                _structuredLogger?.LogError("PublishBatch", ex);
                TelemetryHelper.Execute(TelemetryLevels.Error, "rabbitmq-client-publish-batch-failure", ex);
                throw;
            }

            return results;
        }

        public virtual Task<IEnumerable<string>> PublishBatchAsync(IEnumerable<(object Message, string RoutingKey)> messages, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => PublishBatch(messages), cancellationToken);
        }

        private string PublishInternal(
            object message, 
            string routingKey, 
            string? tokenDefault,
            byte? priority,
            IDictionary<string, object>? headers,
            int? ttlMilliseconds)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-client-publish-start", $"token:{tokenDefault}");

                if (!routingKey.HasValue() && !Options.RoutingKey.HasValue())
                {
                    throw new ArgumentNullException(nameof(routingKey), "RoutingKey is required if you do not provide a default route in the configuration.");
                }

                if (!tokenDefault.HasValue())
                {
                    tokenDefault = Guid.NewGuid().ToString();
                }

                if (!Connection.IsConnected)
                {
                    Connection.TryConnect();
                }

                var policy = RetryPolicy.Handle<BrokerUnreachableException>()
                    .Or<SocketException>()
                    .WaitAndRetry(Connection.Options.RetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                    {
                        TelemetryHelper.Execute(TelemetryLevels.Warning, "rabbitmq-client-publish-tryconnect-waitandretry", $"Could not publish: {tokenDefault} after {time.TotalSeconds:n1}s ({ex.Message})");
                    });

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-client-publish-channel-creating", $"token:{tokenDefault}");

                using (var channel = Connection.CreateModel())
                {
                    _metrics?.IncrementChannelCreations();
                    ExchangeDeclare(channel, Options);

                    var bsEvent = message.ToBusinessEvent(tokenDefault: tokenDefault);
                    TelemetryHelper.Execute(TelemetryLevels.Information, "rabbitmq-client-publish-token", tokenDefault);
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-client-publish-body-created", $"token:{tokenDefault}");
                    var body = Encoding.UTF8.GetBytes(bsEvent.ToSerialize(JsonHelper.JsonBusinessEventSettings()));

                    policy.Execute(() =>
                    {
                        var properties = channel.CreateBasicProperties();
                        properties.DeliveryMode = 2;
                        properties.ContentType = "application/json";
                        properties.CorrelationId = tokenDefault;
                        properties.MessageId = tokenDefault;
                        SetRedeliveredCount(properties, 0);

                        // Priority support
                        if (priority.HasValue || (Options.PriorityQueue.Enabled && Options.PriorityQueue.DefaultPriority > 0))
                        {
                            properties.Priority = priority ?? Options.PriorityQueue.DefaultPriority;
                        }

                        // TTL support
                        if (ttlMilliseconds.HasValue || (Options.MessageTtl.Enabled && Options.MessageTtl.DefaultTtlMilliseconds > 0))
                        {
                            properties.Expiration = (ttlMilliseconds ?? Options.MessageTtl.DefaultTtlMilliseconds).ToString();
                        }

                        // Headers support
                        if (headers != null || Options.HeadersExchange.DefaultMessageHeaders != null)
                        {
                            properties.Headers ??= new Dictionary<string, object>();
                            
                            if (Options.HeadersExchange.DefaultMessageHeaders != null)
                            {
                                foreach (var h in Options.HeadersExchange.DefaultMessageHeaders)
                                {
                                    properties.Headers[h.Key] = h.Value;
                                }
                            }
                            
                            if (headers != null)
                            {
                                foreach (var h in headers)
                                {
                                    properties.Headers[h.Key] = h.Value;
                                }
                            }
                        }

                        TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-client-publish-sending", $"token:{tokenDefault}");

                        if (Options.PublisherConfirm.Enabled)
                        {
                            channel.ConfirmSelect();
                        }
                        
                        channel.BasicPublish(exchange: Options.Exchange,
                                         routingKey: routingKey ?? Options.RoutingKey ?? string.Empty,
                                         basicProperties: Options.BasicProperties ?? properties,
                                         body: body);
                        
                        if (Options.PublisherConfirm.Enabled)
                        {
                            if (Options.PublisherConfirm.WaitForConfirmsOrDie)
                            {
                                channel.WaitForConfirmsOrDie(TimeSpan.FromMilliseconds(Options.PublisherConfirm.TimeoutMilliseconds));
                            }
                            else
                            {
                                channel.WaitForConfirms(TimeSpan.FromMilliseconds(Options.PublisherConfirm.TimeoutMilliseconds));
                            }
                            _metrics?.IncrementPublisherConfirms();
                        }
                    });

                    stopwatch.Stop();
                    _metrics?.IncrementMessagesSent(Options.Exchange);
                    
                    _structuredLogger?.LogMessagePublished(
                        tokenDefault,
                        Options.Exchange,
                        routingKey ?? Options.RoutingKey ?? string.Empty,
                        body.Length,
                        headers,
                        priority,
                        stopwatch.Elapsed);

                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-client-publish-success", $"token:{tokenDefault}");
                    TelemetryHelper.Execute(TelemetryLevels.Information, "rabbitmq-client-publish-success", $"token:{tokenDefault}");
                }

                return tokenDefault;
            }
            catch (Exception ex)
            {
                _metrics?.IncrementError(ex.GetType().Name);
                _structuredLogger?.LogError("Publish", ex, tokenDefault);
                TelemetryHelper.Execute(TelemetryLevels.Error, "rabbitmq-client-publish-failure", ex);
                throw;
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-client-publish-end", $"token:{tokenDefault}");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "Low complexity")]
        public virtual void Consume()
        {
            try
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-consumer-start");

                if (!_consumers.AnySafe())
                {
                    throw new ArgumentException("We didn't find consumers registered through the configuration.");
                }

                if (Connection.Options.DispatchConsumersAsync)
                {
                    if (_consumers.AnySafe(x => x.InheritsOrImplements(typeof(IMvpRabbitMQConsumerSync))))
                    {
                        throw new ArgumentException("DispatchConsumersAsync is enabled, so register only classes that implement the IMvpRabbitMQConsumerAsync interface.");
                    }
                }
                else
                {
                    if (_consumers.AnySafe(x => x.InheritsOrImplements(typeof(IMvpRabbitMQConsumerAsync))))
                    {
                        throw new ArgumentException("DispatchConsumersAsync is disabled, so register only classes that implement the IMvpRabbitMQConsumerSync interface.");
                    }
                }

                foreach (var item in _consumers)
                {
                    var consumer = GetService(item);
                    if (consumer == null) continue;
                    var channel = CreateConsumerChannel(QueueBind, consumer.RoutingKey, consumer.QueueName);

                    // dead letter
                    if (Options.DeadLetter != null)
                    {
                        ExchangeDeclare(channel, Options.DeadLetter);
                        QueueDeclare(channel, Options.DeadLetter, $"dead-letter-{consumer.QueueName}");
                        QueueBind(channel, Options.DeadLetter, consumer.RoutingKey, $"dead-letter-{consumer.QueueName}");
                    }

                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-consumer-event", $"channel-number:{channel.ChannelNumber}");
                    IBasicConsumer _event;

                    if (Connection.Options.DispatchConsumersAsync)
                    {
                        var _eventAsync = new AsyncEventingBasicConsumer(channel);
                        _eventAsync.Received += async (sender, e) =>
                        {
                            TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-consumer-received");
                            await HandleConsumeAsync(e, channel, (IMvpRabbitMQConsumerAsync)consumer);
                        };
                        _event = _eventAsync;
                    }
                    else
                    {
                        var _eventSync = new EventingBasicConsumer(channel);
                        _eventSync.Received += (sender, e) =>
                        {
                            TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-consumer-received");
                            HandleConsume(e, channel, (IMvpRabbitMQConsumerSync)consumer);
                        };
                        _event = _eventSync;
                    }

                    // Apply QoS settings
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-consumer-basic-qos", $"prefetchSize:{Options.ConsumerPrefetch.PrefetchSize}|prefetchCount:{Options.ConsumerPrefetch.PrefetchCount}|global:{Options.ConsumerPrefetch.Global}");
                    channel.BasicQos(
                        prefetchSize: Options.ConsumerPrefetch.PrefetchSize, 
                        prefetchCount: Options.ConsumerPrefetch.PrefetchCount, 
                        global: Options.ConsumerPrefetch.Global);

                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-consumer-basic", $"queue:{consumer.QueueName ?? Options.QueueName ?? string.Empty}|autoAck: false");
                    channel.BasicConsume(queue: consumer.QueueName ?? Options.QueueName ?? string.Empty,
                         autoAck: false,
                         consumer: _event);
                }
            }
            catch (Exception ex)
            {
                _metrics?.IncrementError(ex.GetType().Name);
                _structuredLogger?.LogError("Consume", ex);
                TelemetryHelper.Execute(TelemetryLevels.Error, "rabbitmq-consumer-failure", ex);
                throw;
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-consumer-end");
            }
        }

        private IModel CreateConsumerChannel(Action<IModel, RabbitMQClientOptions, string, string> action, string routingKey, string queueName)
        {
            string queueKeyName = queueName ?? Options.QueueName ?? string.Empty;
            IModel? channel = null;

            if (Channels.TryGetValue(queueKeyName, out IModel? value))
            {
                channel = value;
            }

            if (channel == null || channel.IsClosed)
            {
                if (!Connection.IsConnected)
                {
                    Connection.TryConnect();
                }

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-channel-creating");
                channel = Connection.CreateModel();
                _metrics?.IncrementChannelCreations();
                _structuredLogger?.LogChannelEvent("Created", channel.ChannelNumber);

                ExchangeDeclare(channel, Options);
                QueueDeclare(channel, Options, queueKeyName);

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-channel-created", $"channel-number:{channel.ChannelNumber}");

                channel.CallbackException += (sender, ea) =>
                {
                    if (!channel.IsOpen)
                        channel.Dispose();
                    channel = CreateConsumerChannel(action, routingKey, queueName);
                    TelemetryHelper.Execute(TelemetryLevels.Warning, "rabbitmq-channel-recreating", $"channel-number:{channel.ChannelNumber}");
                };

                action?.Invoke(channel, Options, routingKey, queueName);

                Channels.TryAdd(queueKeyName, channel);
            }
            return channel;
        }

        private void ExchangeDeclare(IModel channelCtx, RabbitMQOptions optionsCtx)
        {
            if (channelCtx == null || optionsCtx == null) return;

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-channel-exchange-declaring", $"exchange:{optionsCtx.Exchange}|type:{optionsCtx.ExchangeType}|durable:{optionsCtx.Durable}|autoDelete:{optionsCtx.AutoDelete}");
            channelCtx.ExchangeDeclare(
                exchange: optionsCtx.Exchange,
                type: optionsCtx.ExchangeType.ToString(),
                durable: optionsCtx.Durable,
                autoDelete: optionsCtx.AutoDelete,
                arguments: optionsCtx.ExchangeArguments
            );
            
            _structuredLogger?.LogExchangeDeclared(
                optionsCtx.Exchange,
                optionsCtx.ExchangeType.ToString(),
                optionsCtx.Durable,
                optionsCtx.AutoDelete);
        }

        private void QueueDeclare(IModel channelCtx, RabbitMQOptions optionsCtx, string? queueName = null)
        {
            if (channelCtx == null || optionsCtx == null) return;

            var arguments = optionsCtx.QueueArguments ?? new Dictionary<string, object>();

            // Priority queue support
            if (Options.PriorityQueue.Enabled)
            {
                arguments["x-max-priority"] = (int)Options.PriorityQueue.MaxPriority;
            }

            // TTL support
            if (Options.MessageTtl.Enabled)
            {
                if (Options.MessageTtl.QueueTtlMilliseconds > 0)
                {
                    arguments["x-message-ttl"] = Options.MessageTtl.QueueTtlMilliseconds;
                }
                if (Options.MessageTtl.QueueExpiresMilliseconds > 0)
                {
                    arguments["x-expires"] = Options.MessageTtl.QueueExpiresMilliseconds;
                }
            }

            // Dead letter exchange
            if (Options.DeadLetter != null)
            {
                arguments["x-dead-letter-exchange"] = Options.DeadLetter.Exchange;
                if (Options.DeadLetter.RoutingKey.HasValue())
                {
                    arguments["x-dead-letter-routing-key"] = Options.DeadLetter.RoutingKey;
                }
            }

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-channel-queue-setting", $"queue:{queueName ?? optionsCtx.QueueName ?? string.Empty}|durable:{optionsCtx.Durable}|exclusive:{optionsCtx.Exclusive}|autoDelete:{optionsCtx.AutoDelete}");
            var result = channelCtx.QueueDeclare(
                queue: queueName ?? optionsCtx.QueueName ?? string.Empty,
                durable: optionsCtx.Durable,
                exclusive: optionsCtx.Exclusive,
                autoDelete: optionsCtx.AutoDelete,
                arguments: arguments.Count > 0 ? arguments : null
            );
            
            _structuredLogger?.LogQueueDeclared(
                queueName ?? optionsCtx.QueueName ?? string.Empty,
                optionsCtx.Durable,
                optionsCtx.Exclusive,
                optionsCtx.AutoDelete,
                (int?)result?.MessageCount);
        }

        private void QueueBind(IModel channelCtx, RabbitMQClientOptions optionsCtx, string routingKey, string queueName)
        {
            QueueBindInternal(channelCtx, optionsCtx, routingKey, queueName, 
                optionsCtx.HeadersExchange.Enabled ? optionsCtx.HeadersExchange : null);
        }

        private void QueueBind(IModel channelCtx, RabbitMQOptions optionsCtx, string routingKey, string queueName)
        {
            QueueBindInternal(channelCtx, optionsCtx, routingKey, queueName, null);
        }

        private void QueueBindInternal(IModel channelCtx, RabbitMQOptions optionsCtx, string routingKey, string queueName, HeadersExchangeOptions? headersExchange)
        {
            if (channelCtx == null || optionsCtx == null) return;

            IDictionary<string, object>? bindingArgs = null;

            // Headers exchange binding
            if (headersExchange?.Enabled == true && headersExchange.BindingHeaders != null)
            {
                bindingArgs = new Dictionary<string, object>(headersExchange.BindingHeaders)
                {
                    ["x-match"] = headersExchange.MatchType
                };
            }

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-consumer-queue-bind", $"queue:{queueName ?? optionsCtx.QueueName ?? string.Empty}|exchange:{optionsCtx.Exchange}|routingKey:{routingKey ?? optionsCtx.RoutingKey}");
            channelCtx.QueueBind(
                queue: queueName ?? optionsCtx.QueueName ?? string.Empty,
                exchange: optionsCtx.Exchange,
                routingKey: routingKey ?? optionsCtx.RoutingKey ?? string.Empty,
                arguments: bindingArgs);
        }

        private void HandleConsume(BasicDeliverEventArgs e, IModel channel, IMvpRabbitMQConsumerSync consumerSync)
        {
            var stopwatch = Stopwatch.StartNew();
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-consumer-received-start");
            string? token = null;
            object? data = null;
            int redeliveredCount = 0;
            IBasicProperties? properties = null;
            try
            {
                properties = e.BasicProperties;
                redeliveredCount = GetRedeliveredCount(properties);
                var messageId = properties.MessageId ?? properties.CorrelationId ?? Guid.NewGuid().ToString();

                // Deduplication check
                if (_deduplicationStore != null && Options.Deduplication.Enabled)
                {
                    var isProcessed = _deduplicationStore.IsProcessedAsync(messageId).GetAwaiter().GetResult();
                    if (isProcessed)
                    {
                        _metrics?.IncrementDuplicateMessagesSkipped();
                        _structuredLogger?.LogDuplicateMessageSkipped(messageId);
                        TelemetryHelper.Execute(TelemetryLevels.Information, "rabbitmq-consumer-duplicate-skipped", $"messageId:{messageId}");
                        BasicAck(e, channel);
                        return;
                    }
                }

                IBusinessEvent bsEvent = ExtractBodyToBusinessEvent(e);
                token = bsEvent.Token;
                
                _metrics?.IncrementMessagesReceived(consumerSync.QueueName);
                _structuredLogger?.LogMessageReceived(
                    messageId,
                    e.Exchange,
                    e.RoutingKey,
                    e.ConsumerTag,
                    e.Redelivered,
                    e.Body.Length,
                    properties.Headers);

                if (redeliveredCount > 1)
                {
                    _metrics?.IncrementMessagesRedelivered();
                    _structuredLogger?.LogMessageRedelivered(messageId, redeliveredCount, Options.MaxRedeliveredCount);
                }

                TelemetryHelper.Execute(TelemetryLevels.Information, "rabbitmq-consumer-received-token", token);
                TelemetryHelper.Execute(TelemetryLevels.Information, "rabbitmq-consumer-received-redelivered", $"count:{redeliveredCount}|token:{token}");

                data = bsEvent.GetDataObject();
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-consumer-received-dispatching-start", $"token:{token}");
                try
                {
                    consumerSync.Received(data, token);
                }
                finally
                {
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-consumer-received-dispatching-end", $"token:{token}");
                }

                // Mark as processed for deduplication
                if (_deduplicationStore != null && Options.Deduplication.Enabled)
                {
                    _deduplicationStore.MarkAsProcessedAsync(messageId, 
                        DateTimeOffset.UtcNow.AddMinutes(Options.Deduplication.ExpirationMinutes))
                        .GetAwaiter().GetResult();
                }

                stopwatch.Stop();
                BasicAck(e, channel, messageId, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                try
                {
                    _metrics?.IncrementError(ex.GetType().Name);
                    _structuredLogger?.LogError("HandleConsume", ex, token);
                    TelemetryHelper.Execute(TelemetryLevels.Error, "rabbitmq-consumer-received-failure", ex);

                    if (consumerSync is IMvpRabbitMQConsumerRecoverySync recoverySync)
                        recoverySync.Failure(ex, token);

                    if (redeliveredCount < Options.MaxRedeliveredCount)
                    {
                        TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-consumer-received-redelivered", $"count:{redeliveredCount}");
                        SetRedeliveredCount(properties!, redeliveredCount);
                        channel.BasicPublish(e.Exchange, e.RoutingKey, properties, e.Body);
                        BasicAck(e, channel);
                    }
                    else
                    {
                        TelemetryHelper.Execute(TelemetryLevels.Error, "rabbitmq-consumer-received-reject", $"redelivered-count:{redeliveredCount}");
                        if (consumerSync is IMvpRabbitMQConsumerRecoverySync rejectedSync)
                            rejectedSync.Rejected(data, token);
                        BasicNack(e, channel, token ?? "unknown");
                    }
                }
                catch (Exception)
                {
                    BasicNack(e, channel, token ?? "unknown");
                }
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-consumer-received-end");
            }
        }

        private async Task HandleConsumeAsync(BasicDeliverEventArgs e, IModel channel, IMvpRabbitMQConsumerAsync consumerAsync)
        {
            var stopwatch = Stopwatch.StartNew();
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-consumer-received-start");
            string? token = null;
            object? data = null;
            int redeliveredCount = 0;
            IBasicProperties? properties = null;
            try
            {
                properties = e.BasicProperties;
                redeliveredCount = GetRedeliveredCount(properties);
                var messageId = properties.MessageId ?? properties.CorrelationId ?? Guid.NewGuid().ToString();

                // Deduplication check
                if (_deduplicationStore != null && Options.Deduplication.Enabled)
                {
                    var isProcessed = await _deduplicationStore.IsProcessedAsync(messageId);
                    if (isProcessed)
                    {
                        _metrics?.IncrementDuplicateMessagesSkipped();
                        _structuredLogger?.LogDuplicateMessageSkipped(messageId);
                        TelemetryHelper.Execute(TelemetryLevels.Information, "rabbitmq-consumer-duplicate-skipped", $"messageId:{messageId}");
                        BasicAck(e, channel);
                        return;
                    }
                }

                IBusinessEvent bsEvent = ExtractBodyToBusinessEvent(e);
                token = bsEvent.Token;
                
                _metrics?.IncrementMessagesReceived(consumerAsync.QueueName);
                _structuredLogger?.LogMessageReceived(
                    messageId,
                    e.Exchange,
                    e.RoutingKey,
                    e.ConsumerTag,
                    e.Redelivered,
                    e.Body.Length,
                    properties.Headers);

                if (redeliveredCount > 1)
                {
                    _metrics?.IncrementMessagesRedelivered();
                    _structuredLogger?.LogMessageRedelivered(messageId, redeliveredCount, Options.MaxRedeliveredCount);
                }

                TelemetryHelper.Execute(TelemetryLevels.Information, "rabbitmq-consumer-received-token", token);
                TelemetryHelper.Execute(TelemetryLevels.Information, "rabbitmq-consumer-received-redelivered", $"count:{redeliveredCount}|token:{token}");

                data = bsEvent.GetDataObject();

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-consumer-received-dispatching-start", $"token:{token}");
                try
                {
                    await consumerAsync.ReceivedAsync(data, token);
                }
                finally
                {
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-consumer-received-dispatching-end", $"token:{token}");
                }

                // Mark as processed for deduplication
                if (_deduplicationStore != null && Options.Deduplication.Enabled)
                {
                    await _deduplicationStore.MarkAsProcessedAsync(messageId, 
                        DateTimeOffset.UtcNow.AddMinutes(Options.Deduplication.ExpirationMinutes));
                }

                stopwatch.Stop();
                BasicAck(e, channel, messageId, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                try
                {
                    _metrics?.IncrementError(ex.GetType().Name);
                    _structuredLogger?.LogError("HandleConsumeAsync", ex, token);
                    TelemetryHelper.Execute(TelemetryLevels.Error, "rabbitmq-consumer-received-failure", ex);
                    
                    if (consumerAsync is IMvpRabbitMQConsumerRecoveryAsync recoveryAsync)
                        await recoveryAsync.FailureAsync(ex, token);

                    if (redeliveredCount < Options.MaxRedeliveredCount)
                    {
                        // requeue
                        TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-consumer-received-redelivered", $"count:{redeliveredCount}");
                        SetRedeliveredCount(properties!, redeliveredCount);
                        channel.BasicPublish(e.Exchange, e.RoutingKey, properties, e.Body);
                        BasicAck(e, channel);
                    }
                    else
                    {
                        TelemetryHelper.Execute(TelemetryLevels.Error, "rabbitmq-consumer-received-reject", $"redelivered-count:{redeliveredCount}");
                        if (consumerAsync is IMvpRabbitMQConsumerRecoveryAsync rejectedAsync)
                            await rejectedAsync.RejectedAsync(data, token);
                        BasicNack(e, channel, token ?? "unknown");
                    }
                }
                catch (Exception)
                {
                    BasicNack(e, channel, token ?? "unknown");
                }
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-consumer-received-end");
            }
        }

        private static int GetRedeliveredCount(IBasicProperties properties)
        {
            int result = (int?)properties.Headers?["x-redelivered-count"] ?? 0;
            return result + 1;
        }

        private static void SetRedeliveredCount(IBasicProperties properties, int count)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-consumer-received-retryaccount");
            properties.Headers ??= new Dictionary<string, object>();
            properties.Headers["x-redelivered-count"] = count;
        }

        private void BasicAck(BasicDeliverEventArgs e, IModel channel, string? messageId = null, TimeSpan? processingTime = null)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-consumer-received-basicack");
            channel.BasicAck(e.DeliveryTag, false);
            _metrics?.IncrementMessagesAcked();
            
            if (messageId != null && processingTime.HasValue)
            {
                _structuredLogger?.LogMessageAcked(messageId, e.DeliveryTag, processingTime.Value);
            }
        }

        private void BasicNack(BasicDeliverEventArgs e, IModel channel, string messageId)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-consumer-received-basicnack");
            channel.BasicNack(e.DeliveryTag, false, false);
            _metrics?.IncrementMessagesNacked();
            _structuredLogger?.LogMessageNacked(messageId, e.DeliveryTag, false, "Max redelivery count exceeded");
        }

        private static IBusinessEvent ExtractBodyToBusinessEvent(BasicDeliverEventArgs e)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-consumer-received-body-extract-start");
            try
            {
                var body = e.Body.ToArray();
                string messageString = Encoding.UTF8.GetString(body);
                IBusinessEvent bsEvent = messageString.ToDeserialize<IBusinessEvent>(JsonHelper.JsonBusinessEventSettings());
                return bsEvent;
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-consumer-received-body-extract-end");
            }
        }

        #endregion

        #region [ Registrars ]
        private IMvpRabbitMQConsumer GetService(Type consumerType)
        {
            if (consumerType.InheritsOrImplements(typeof(IMvpRabbitMQConsumerSync)))
            {
                return (IMvpRabbitMQConsumerSync)(_provider.GetService(consumerType) ?? Activator.CreateInstance(consumerType)!);
            }
            else if (consumerType.InheritsOrImplements(typeof(IMvpRabbitMQConsumerAsync)))
            {
                return (IMvpRabbitMQConsumerAsync)(_provider.GetService(consumerType) ?? Activator.CreateInstance(consumerType)!);
            }
            else if (consumerType.InheritsOrImplements(typeof(IMvpRabbitMQConsumerRecoverySync)))
            {
                return (IMvpRabbitMQConsumerRecoverySync)(_provider.GetService(consumerType) ?? Activator.CreateInstance(consumerType)!);
            }
            else if (consumerType.InheritsOrImplements(typeof(IMvpRabbitMQConsumerRecoveryAsync)))
            {
                return (IMvpRabbitMQConsumerRecoveryAsync)(_provider.GetService(consumerType) ?? Activator.CreateInstance(consumerType)!);
            }
            throw new ArgumentException("Invalid type for consumers.");
        }
        public void Register<T>() where T : class, IMvpRabbitMQConsumer
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-client-consumer-register-generic");
            Register(typeof(T));
        }
        public void Register(Type consumerType)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-client-consumer-register");
            ArgumentNullException.ThrowIfNull(consumerType);
            _consumers.Add(consumerType);
        }
        public void Unregister<T>() where T : class, IMvpRabbitMQConsumer
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-client-consumer-unregister-generic");
            Unregister(typeof(T));
        }
        public void Unregister(Type consumerType)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-client-consumer-unregister");
            ArgumentNullException.ThrowIfNull(consumerType);
            _consumers
                .FindAll(x => x.InheritsOrImplements(consumerType))
                .ForEach(item => _consumers.Remove(item));
        }
        #endregion
    }
}
