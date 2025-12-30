//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Channels;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Infrastructure.Channels;

/// <summary>
/// High-level producer/consumer pattern implementation using System.Threading.Channels.
/// </summary>
/// <typeparam name="TItem">The type of items to process.</typeparam>
/// <remarks>
/// <para>
/// This class provides a simple, high-level API for implementing the producer/consumer
/// pattern with built-in support for:
/// </para>
/// <list type="bullet">
/// <item>Multiple producers and consumers</item>
/// <item>Backpressure with bounded channels</item>
/// <item>Graceful shutdown</item>
/// <item>Error handling</item>
/// <item>Cancellation</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Create a producer-consumer with 4 workers
/// using var pc = new ProducerConsumer&lt;Order&gt;(
///     processor: async (order, ct) => await ProcessOrderAsync(order, ct),
///     workerCount: 4,
///     options: new ProducerConsumerOptions { Capacity = 100 });
/// 
/// // Start processing
/// pc.Start();
/// 
/// // Produce items
/// await pc.ProduceAsync(new Order { Id = 1 });
/// await pc.ProduceAsync(new Order { Id = 2 });
/// 
/// // Wait for completion
/// pc.Complete();
/// await pc.WaitForCompletionAsync();
/// </code>
/// </example>
public sealed class ProducerConsumer<TItem> : IAsyncDisposable
{
    private readonly Func<TItem, CancellationToken, Task> _processor;
    private readonly Channel<TItem> _channel;
    private readonly int _workerCount;
    private readonly ILogger<ProducerConsumer<TItem>>? _logger;
    private readonly ProducerConsumerOptions _options;
    private readonly CancellationTokenSource _cts = new();
    private readonly List<Task> _workerTasks = [];
    private bool _started;
    private bool _completed;
    private bool _disposed;

    /// <summary>
    /// Creates a new producer-consumer.
    /// </summary>
    /// <param name="processor">The function to process each item.</param>
    /// <param name="workerCount">The number of consumer workers. Default is processor count.</param>
    /// <param name="options">Optional configuration options.</param>
    /// <param name="logger">Optional logger.</param>
    public ProducerConsumer(
        Func<TItem, CancellationToken, Task> processor,
        int? workerCount = null,
        ProducerConsumerOptions? options = null,
        ILogger<ProducerConsumer<TItem>>? logger = null)
    {
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        _workerCount = workerCount ?? Environment.ProcessorCount;
        _options = options ?? new ProducerConsumerOptions();
        _logger = logger;

        _channel = _options.IsBounded
            ? Channel.CreateBounded<TItem>(new BoundedChannelOptions(_options.Capacity)
            {
                FullMode = _options.FullMode,
                SingleReader = _workerCount == 1,
                SingleWriter = false,
                AllowSynchronousContinuations = _options.AllowSynchronousContinuations
            })
            : Channel.CreateUnbounded<TItem>(new UnboundedChannelOptions
            {
                SingleReader = _workerCount == 1,
                SingleWriter = false,
                AllowSynchronousContinuations = _options.AllowSynchronousContinuations
            });
    }

    /// <summary>
    /// Starts the consumer workers.
    /// </summary>
    public void Start()
    {
        if (_started) return;
        _started = true;

        _logger?.LogInformation(
            "Starting {WorkerCount} consumer workers for {ItemType}",
            _workerCount,
            typeof(TItem).Name);

        for (var i = 0; i < _workerCount; i++)
        {
            var workerId = i;
            _workerTasks.Add(Task.Run(() => ConsumeAsync(workerId, _cts.Token), _cts.Token));
        }
    }

    /// <summary>
    /// Produces an item to be processed.
    /// </summary>
    /// <param name="item">The item to produce.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async ValueTask ProduceAsync(TItem item, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_completed)
            throw new InvalidOperationException("Cannot produce after calling Complete()");

        await _channel.Writer.WriteAsync(item, cancellationToken);
    }

    /// <summary>
    /// Produces multiple items to be processed.
    /// </summary>
    /// <param name="items">The items to produce.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async ValueTask ProduceManyAsync(
        IEnumerable<TItem> items,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
        {
            await ProduceAsync(item, cancellationToken);
        }
    }

    /// <summary>
    /// Produces items from an async enumerable.
    /// </summary>
    /// <param name="items">The async enumerable of items.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async ValueTask ProduceManyAsync(
        IAsyncEnumerable<TItem> items,
        CancellationToken cancellationToken = default)
    {
        await foreach (var item in items.WithCancellation(cancellationToken))
        {
            await ProduceAsync(item, cancellationToken);
        }
    }

    /// <summary>
    /// Signals that no more items will be produced.
    /// </summary>
    public void Complete()
    {
        if (_completed) return;
        _completed = true;

        _channel.Writer.Complete();
        _logger?.LogInformation("Producer completed, waiting for consumers to finish");
    }

    /// <summary>
    /// Waits for all items to be processed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task WaitForCompletionAsync(CancellationToken cancellationToken = default)
    {
        if (!_started) Start();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);
        await Task.WhenAll(_workerTasks);

        _logger?.LogInformation("All consumer workers completed");
    }

    /// <summary>
    /// Runs a producer function and waits for all items to be processed.
    /// </summary>
    /// <param name="producer">The producer function.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RunAsync(
        Func<ProducerConsumer<TItem>, CancellationToken, Task> producer,
        CancellationToken cancellationToken = default)
    {
        Start();

        try
        {
            await producer(this, cancellationToken);
        }
        finally
        {
            Complete();
        }

        await WaitForCompletionAsync(cancellationToken);
    }

    private async Task ConsumeAsync(int workerId, CancellationToken cancellationToken)
    {
        _logger?.LogDebug("Consumer worker {WorkerId} started", workerId);

        try
        {
            await foreach (var item in _channel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    await _processor(item, cancellationToken);
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger?.LogError(ex,
                        "Error processing item in worker {WorkerId}",
                        workerId);

                    if (!_options.ContinueOnError)
                        throw;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger?.LogDebug("Consumer worker {WorkerId} cancelled", workerId);
        }
        finally
        {
            _logger?.LogDebug("Consumer worker {WorkerId} stopped", workerId);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _channel.Writer.TryComplete();
        _cts.Cancel();

        try
        {
            await Task.WhenAll(_workerTasks);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Options for configuring a producer-consumer.
/// </summary>
public class ProducerConsumerOptions
{
    /// <summary>
    /// Gets or sets whether the channel is bounded.
    /// Default is true for backpressure support.
    /// </summary>
    public bool IsBounded { get; set; } = true;

    /// <summary>
    /// Gets or sets the capacity of the bounded channel.
    /// Default is 100.
    /// </summary>
    public int Capacity { get; set; } = 100;

    /// <summary>
    /// Gets or sets the behavior when the channel is full.
    /// Default is Wait.
    /// </summary>
    public BoundedChannelFullMode FullMode { get; set; } = BoundedChannelFullMode.Wait;

    /// <summary>
    /// Gets or sets whether to continue processing on errors.
    /// Default is true.
    /// </summary>
    public bool ContinueOnError { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to allow synchronous continuations.
    /// Default is false.
    /// </summary>
    public bool AllowSynchronousContinuations { get; set; } = false;
}

/// <summary>
/// Producer-consumer with result collection.
/// </summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TOutput">The output type.</typeparam>
public sealed class ProducerConsumer<TInput, TOutput> : IAsyncDisposable
{
    private readonly Func<TInput, CancellationToken, Task<TOutput>> _processor;
    private readonly Channel<TInput> _inputChannel;
    private readonly Channel<TOutput> _outputChannel;
    private readonly int _workerCount;
    private readonly ILogger? _logger;
    private readonly ProducerConsumerOptions _options;
    private readonly CancellationTokenSource _cts = new();
    private readonly List<Task> _workerTasks = [];
    private bool _started;
    private bool _completed;
    private bool _disposed;

    /// <summary>
    /// Creates a new producer-consumer with results.
    /// </summary>
    public ProducerConsumer(
        Func<TInput, CancellationToken, Task<TOutput>> processor,
        int? workerCount = null,
        ProducerConsumerOptions? options = null,
        ILogger? logger = null)
    {
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        _workerCount = workerCount ?? Environment.ProcessorCount;
        _options = options ?? new ProducerConsumerOptions();
        _logger = logger;

        _inputChannel = _options.IsBounded
            ? Channel.CreateBounded<TInput>(_options.Capacity)
            : Channel.CreateUnbounded<TInput>();

        _outputChannel = _options.IsBounded
            ? Channel.CreateBounded<TOutput>(_options.Capacity)
            : Channel.CreateUnbounded<TOutput>();
    }

    /// <summary>
    /// Starts the consumer workers.
    /// </summary>
    public void Start()
    {
        if (_started) return;
        _started = true;

        for (var i = 0; i < _workerCount; i++)
        {
            _workerTasks.Add(Task.Run(() => ConsumeAsync(_cts.Token), _cts.Token));
        }

        _ = Task.WhenAll(_workerTasks).ContinueWith(_ =>
            _outputChannel.Writer.TryComplete(), _cts.Token);
    }

    /// <summary>
    /// Produces an item to be processed.
    /// </summary>
    public async ValueTask ProduceAsync(TInput item, CancellationToken cancellationToken = default)
    {
        await _inputChannel.Writer.WriteAsync(item, cancellationToken);
    }

    /// <summary>
    /// Signals that no more items will be produced.
    /// </summary>
    public void Complete()
    {
        if (_completed) return;
        _completed = true;
        _inputChannel.Writer.Complete();
    }

    /// <summary>
    /// Gets the output items as an async enumerable.
    /// </summary>
    public IAsyncEnumerable<TOutput> GetResultsAsync(CancellationToken cancellationToken = default)
    {
        return _outputChannel.Reader.ReadAllAsync(cancellationToken);
    }

    private async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        await foreach (var item in _inputChannel.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                var result = await _processor(item, cancellationToken);
                await _outputChannel.Writer.WriteAsync(result, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger?.LogError(ex, "Error processing item");
                if (!_options.ContinueOnError) throw;
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _inputChannel.Writer.TryComplete();
        _cts.Cancel();

        try
        {
            await Task.WhenAll(_workerTasks);
        }
        catch (OperationCanceledException) { }

        _cts.Dispose();
    }
}

