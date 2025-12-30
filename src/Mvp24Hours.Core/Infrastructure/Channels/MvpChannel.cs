//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Channels;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Infrastructure.Channels;

/// <summary>
/// Implementation of <see cref="IChannel{T}"/> using System.Threading.Channels.
/// </summary>
/// <typeparam name="T">The type of items in the channel.</typeparam>
/// <remarks>
/// <para>
/// This implementation wraps the native .NET <see cref="Channel{T}"/> to provide
/// a consistent abstraction for producer/consumer patterns with additional features
/// like batch reading and message tracking.
/// </para>
/// <para>
/// <strong>Key Features:</strong>
/// <list type="bullet">
/// <item>Bounded channels with backpressure support</item>
/// <item>Unbounded channels for fire-and-forget scenarios</item>
/// <item>Batch reading for efficient processing</item>
/// <item>Async enumerable support for streaming</item>
/// <item>Thread-safe operations</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a bounded channel with backpressure
/// using var channel = new MvpChannel&lt;Order&gt;(MvpChannelOptions.Bounded(100));
/// 
/// // Producer
/// await channel.Writer.WriteAsync(new Order { Id = 1, Product = "Widget" });
/// 
/// // Consumer
/// await foreach (var order in channel.Reader.ReadAllAsync())
/// {
///     await ProcessOrderAsync(order);
/// }
/// 
/// // Batch processing
/// await foreach (var batch in channel.Reader.ReadBatchAsync(batchSize: 10, timeout: TimeSpan.FromSeconds(5)))
/// {
///     await ProcessBatchAsync(batch);
/// }
/// </code>
/// </example>
public sealed class MvpChannel<T> : IChannel<T>
{
    private readonly Channel<T> _channel;
    private readonly MvpChannelReader<T> _reader;
    private readonly MvpChannelWriter<T> _writer;
    private readonly MvpChannelOptions _options;
    private bool _disposed;

    /// <summary>
    /// Creates a new MvpChannel with the specified options.
    /// </summary>
    /// <param name="options">The channel options. Defaults to bounded channel with 100 capacity.</param>
    public MvpChannel(MvpChannelOptions? options = null)
    {
        _options = options ?? new MvpChannelOptions();
        _channel = CreateChannel(_options);
        _reader = new MvpChannelReader<T>(_channel.Reader, _options);
        _writer = new MvpChannelWriter<T>(_channel.Writer, _options);
    }

    /// <inheritdoc />
    public IChannelReader<T> Reader => _reader;

    /// <inheritdoc />
    public IChannelWriter<T> Writer => _writer;

    /// <inheritdoc />
    public bool IsCompleted => _reader.IsCompleted;

    /// <inheritdoc />
    public int Count => _reader.Count;

    /// <inheritdoc />
    public MvpChannelOptions Options => _options;

    /// <summary>
    /// Creates the underlying channel based on options.
    /// </summary>
    private static Channel<T> CreateChannel(MvpChannelOptions options)
    {
        if (options.IsBounded)
        {
            return Channel.CreateBounded<T>(new BoundedChannelOptions(options.Capacity)
            {
                FullMode = options.FullMode,
                AllowSynchronousContinuations = options.AllowSynchronousContinuations,
                SingleReader = options.SingleReader,
                SingleWriter = options.SingleWriter
            });
        }

        return Channel.CreateUnbounded<T>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = options.AllowSynchronousContinuations,
            SingleReader = options.SingleReader,
            SingleWriter = options.SingleWriter
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _writer.TryComplete();
    }

    /// <summary>
    /// Creates an unbounded channel.
    /// </summary>
    /// <returns>A new unbounded channel.</returns>
    public static MvpChannel<T> CreateUnbounded() => new(MvpChannelOptions.Unbounded());

    /// <summary>
    /// Creates a bounded channel with the specified capacity.
    /// </summary>
    /// <param name="capacity">The maximum capacity.</param>
    /// <returns>A new bounded channel.</returns>
    public static MvpChannel<T> CreateBounded(int capacity) => new(MvpChannelOptions.Bounded(capacity));
}

/// <summary>
/// Implementation of <see cref="IChannelReader{T}"/> wrapping ChannelReader.
/// </summary>
internal sealed class MvpChannelReader<T> : IChannelReader<T>
{
    private readonly ChannelReader<T> _reader;
    private readonly MvpChannelOptions _options;

    public MvpChannelReader(ChannelReader<T> reader, MvpChannelOptions options)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public bool IsCompleted => _reader.Completion.IsCompleted;

    public int Count => _reader.CanCount ? _reader.Count : -1;

    public bool TryRead(out T? item)
    {
        if (_reader.TryRead(out var result))
        {
            item = result;
            return true;
        }
        item = default;
        return false;
    }

    public async ValueTask<T> ReadAsync(CancellationToken cancellationToken = default)
    {
        if (_options.ReadTimeout.HasValue)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.ReadTimeout.Value);
            return await _reader.ReadAsync(cts.Token);
        }

        return await _reader.ReadAsync(cancellationToken);
    }

    public ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
        => _reader.WaitToReadAsync(cancellationToken);

    public IAsyncEnumerable<T> ReadAllAsync(CancellationToken cancellationToken = default)
        => _reader.ReadAllAsync(cancellationToken);

    public async IAsyncEnumerable<IReadOnlyList<T>> ReadBatchAsync(
        int batchSize,
        TimeSpan? timeout = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than 0.");

        var batch = new List<T>(batchSize);
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(30);
        var shouldBreak = false;
        IReadOnlyList<T>? pendingBatch = null;

        while (!cancellationToken.IsCancellationRequested && !shouldBreak)
        {
            var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var timedOut = false;
            var channelCompleted = false;

            try
            {
                timeoutCts.CancelAfter(effectiveTimeout);

                while (batch.Count < batchSize)
                {
                    // Try to read immediately first
                    if (_reader.TryRead(out var item))
                    {
                        batch.Add(item);
                        continue;
                    }

                    // Wait for data with timeout
                    if (await _reader.WaitToReadAsync(timeoutCts.Token))
                    {
                        if (_reader.TryRead(out item))
                        {
                            batch.Add(item);
                        }
                    }
                    else
                    {
                        // Channel completed
                        channelCompleted = true;
                        break;
                    }
                }
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                // Timeout reached
                timedOut = true;
            }
            finally
            {
                timeoutCts.Dispose();
            }

            // Handle channel completion
            if (channelCompleted)
            {
                if (batch.Count > 0)
                {
                    pendingBatch = batch.ToArray();
                    batch.Clear();
                }
                shouldBreak = true;
            }
            // Handle batch full or timeout
            else if (batch.Count >= batchSize || timedOut)
            {
                if (batch.Count > 0)
                {
                    pendingBatch = batch.ToArray();
                    batch.Clear();
                }
            }

            // Yield outside try-catch
            if (pendingBatch != null)
            {
                yield return pendingBatch;
                pendingBatch = null;
            }
        }

        // Yield remaining items
        if (batch.Count > 0)
        {
            yield return batch.ToArray();
        }
    }

    public bool TryPeek(out T? item)
    {
        if (_reader.TryPeek(out var result))
        {
            item = result;
            return true;
        }
        item = default;
        return false;
    }
}

/// <summary>
/// Implementation of <see cref="IChannelWriter{T}"/> wrapping ChannelWriter.
/// </summary>
internal sealed class MvpChannelWriter<T> : IChannelWriter<T>
{
    private readonly ChannelWriter<T> _writer;
    private readonly MvpChannelOptions _options;

    public MvpChannelWriter(ChannelWriter<T> writer, MvpChannelOptions options)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public bool IsCompleted { get; private set; }

    public bool TryWrite(T item) => _writer.TryWrite(item);

    public async ValueTask WriteAsync(T item, CancellationToken cancellationToken = default)
    {
        if (_options.WriteTimeout.HasValue)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.WriteTimeout.Value);
            await _writer.WriteAsync(item, cts.Token);
            return;
        }

        await _writer.WriteAsync(item, cancellationToken);
    }

    public ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default)
        => _writer.WaitToWriteAsync(cancellationToken);

    public bool TryComplete(Exception? error = null)
    {
        var result = _writer.TryComplete(error);
        if (result)
        {
            IsCompleted = true;
        }
        return result;
    }

    public async ValueTask WriteManyAsync(IEnumerable<T> items, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WriteAsync(item, cancellationToken);
        }
    }

    public async ValueTask WriteManyAsync(IAsyncEnumerable<T> items, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        await foreach (var item in items.WithCancellation(cancellationToken))
        {
            await WriteAsync(item, cancellationToken);
        }
    }
}

