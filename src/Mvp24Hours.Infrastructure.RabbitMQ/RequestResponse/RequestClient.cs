//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mvp24Hours.Infrastructure.RabbitMQ.RequestResponse;

/// <summary>
/// Implementation of request/response client for RabbitMQ.
/// </summary>
/// <typeparam name="TRequest">The type of the request message.</typeparam>
/// <typeparam name="TResponse">The type of the response message.</typeparam>
/// <remarks>
/// Creates a new request client.
/// </remarks>
public class RequestClient<TRequest, TResponse>(
    IMvpRabbitMQConnection connection,
    IMessageSerializer serializer,
    IOptions<RequestClientOptions>? options = null,
    ILogger<RequestClient<TRequest, TResponse>>? logger = null) : IRequestClient<TRequest, TResponse>, IDisposable
    where TRequest : class
    where TResponse : class
{
    private readonly IMvpRabbitMQConnection _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    private readonly IMessageSerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    private readonly RequestClientOptions _options = options?.Value ?? new RequestClientOptions();
    private readonly ILogger<RequestClient<TRequest, TResponse>>? _logger = logger;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<TResponse>> _pendingRequests = new();
    private IModel? _channel;
    private string? _replyQueueName;
    private bool _disposed;

    /// <inheritdoc />
    public TimeSpan Timeout => TimeSpan.FromMilliseconds(_options.TimeoutMilliseconds);

    /// <inheritdoc />
    public async Task<Response<TResponse>> GetResponseAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        return await GetResponseAsync(request, Timeout, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Response<TResponse>> GetResponseAsync(TRequest request, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        string correlationId = Guid.NewGuid().ToString();

        try
        {
            EnsureChannelAndQueue();

            var tcs = new TaskCompletionSource<TResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingRequests[correlationId] = tcs;

            // Send request
            byte[] body = _serializer.Serialize(request);
            IBasicProperties properties = _channel!.CreateBasicProperties();
            properties.CorrelationId = correlationId;
            properties.ReplyTo = _replyQueueName;
            properties.MessageId = correlationId;
            properties.ContentType = _serializer.ContentType;
            properties.Expiration = timeout.TotalMilliseconds.ToString("F0");

            _channel.BasicPublish(
                exchange: _options.Exchange,
                routingKey: _options.RoutingKey ?? typeof(TRequest).Name,
                basicProperties: properties,
                body: body);

            _logger?.LogDebug(
                "Request sent. CorrelationId={CorrelationId}",
                correlationId);

            // Wait for response
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            try
            {
                TResponse response = await tcs.Task.WaitAsync(cts.Token);
                stopwatch.Stop();

                _logger?.LogInformation(
                    "Request response received. CorrelationId={CorrelationId}, Elapsed={ElapsedMs}ms",
                    correlationId, stopwatch.ElapsedMilliseconds);

                return Response<TResponse>.Success(response, correlationId, stopwatch.Elapsed);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                // Timeout
                stopwatch.Stop();
                _logger?.LogWarning(
                    "Request timeout. CorrelationId={CorrelationId}, Timeout={TimeoutMs}ms",
                    correlationId, timeout.TotalMilliseconds);
                return Response<TResponse>.Timeout(correlationId, stopwatch.Elapsed);
            }
            catch (OperationCanceledException)
            {
                // Cancelled by caller
                stopwatch.Stop();
                return Response<TResponse>.Cancelled(correlationId, stopwatch.Elapsed);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger?.LogError(ex,
                "Request error. CorrelationId={CorrelationId}",
                correlationId);
            return Response<TResponse>.Failure(ex.Message, ex, correlationId, stopwatch.Elapsed);
        }
        finally
        {
            _pendingRequests.TryRemove(correlationId, out _);
        }
    }

    private void EnsureChannelAndQueue()
    {
        if (_channel != null && _channel.IsOpen)
        {
            return;
        }

        if (!_connection.IsConnected)
        {
            _connection.TryConnect();
        }

        _channel = _connection.CreateModel();

        // Declare reply queue (exclusive, auto-delete)
        QueueDeclareOk queueDeclare = _channel.QueueDeclare(
            queue: string.Empty,
            durable: false,
            exclusive: true,
            autoDelete: true,
            arguments: null);

        _replyQueueName = queueDeclare.QueueName;

        // Set up consumer for responses
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += OnResponseReceived;

        _channel.BasicConsume(
            queue: _replyQueueName,
            autoAck: true,
            consumer: consumer);

        _logger?.LogDebug(
            "Request client initialized. ReplyQueue={ReplyQueue}",
            _replyQueueName);
    }

    private void OnResponseReceived(object? sender, BasicDeliverEventArgs e)
    {
        string correlationId = e.BasicProperties.CorrelationId;

        if (string.IsNullOrEmpty(correlationId))
        {
            return;
        }

        if (!_pendingRequests.TryRemove(correlationId, out TaskCompletionSource<TResponse>? tcs))
        {
            return;
        }

        try
        {
            TResponse? response = _serializer.Deserialize<TResponse>(e.Body.ToArray());
            if (response != null)
            {
                tcs.TrySetResult(response);
            }
            else
            {
                tcs.TrySetException(new InvalidOperationException("Failed to deserialize response."));
            }
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }
    }

    /// <summary>
    /// Disposes the request client.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the request client.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // Cancel all pending requests
            foreach (KeyValuePair<string, TaskCompletionSource<TResponse>> kvp in _pendingRequests)
            {
                kvp.Value.TrySetCanceled();
            }
            _pendingRequests.Clear();

            _channel?.Close();
            _channel?.Dispose();
        }

        _disposed = true;
    }
}

