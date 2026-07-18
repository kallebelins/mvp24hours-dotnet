//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;

namespace Mvp24Hours.Infrastructure.Pipe.Operations.Composition;

/// <summary>
/// Represents a sub-pipeline scope that groups operations together.
/// </summary>
/// <remarks>
/// Creates a new sub-pipeline operation.
/// </remarks>
/// <param name="name">Optional name for this scope.</param>
/// <param name="logger">Optional logger for diagnostics.</param>
public class SubPipelineOperation(string? name = null, ILogger<SubPipelineOperation>? logger = null) : IOperation
{
    private readonly List<IOperation> _operations = [];
    private readonly List<IOperation> _executedOperations = [];
    private readonly ILogger<SubPipelineOperation>? _logger = logger;

    /// <summary>
    /// Gets the name of this sub-pipeline scope.
    /// </summary>
    public string? Name { get; } = name;

    /// <inheritdoc />
    public bool IsRequired => false;

    /// <summary>
    /// Gets the operations in this sub-pipeline.
    /// </summary>
    public IReadOnlyList<IOperation> Operations => _operations.AsReadOnly();

    /// <summary>
    /// Adds an operation to this sub-pipeline.
    /// </summary>
    /// <param name="operation">The operation to add.</param>
    /// <returns>This sub-pipeline for chaining.</returns>
    public SubPipelineOperation Add(IOperation operation)
    {
        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        _operations.Add(operation);
        return this;
    }

    /// <inheritdoc />
    public void Execute(IPipelineMessage input)
    {
        string scopeName = Name ?? "anonymous";
        _logger?.LogDebug("SubPipelineOperation: Execute started. Scope: {ScopeName}, Operations: {OperationCount}", scopeName, _operations.Count);
        _executedOperations.Clear();

        try
        {
            foreach (IOperation operation in _operations)
            {
                if (input.IsLocked && !operation.IsRequired)
                {
                    continue;
                }

                _logger?.LogDebug("SubPipelineOperation: Operation '{OperationName}' started. Scope: {ScopeName}", operation.GetType().Name, scopeName);
                try
                {
                    operation.Execute(input);
                    _executedOperations.Add(operation);
                }
                finally
                {
                    _logger?.LogDebug("SubPipelineOperation: Operation '{OperationName}' finished. Scope: {ScopeName}", operation.GetType().Name, scopeName);
                }

                if (input.IsFaulty)
                {
                    break;
                }
            }
        }
        finally
        {
            _logger?.LogDebug("SubPipelineOperation: Execute finished. Scope: {ScopeName}", scopeName);
        }
    }

    /// <inheritdoc />
    public void Rollback(IPipelineMessage input)
    {
        foreach (IOperation operation in _executedOperations.Reverse<IOperation>())
        {
            try
            {
                operation.Rollback(input);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "SubPipelineOperation: Rollback failed");
            }
        }
    }
}

/// <summary>
/// Async version of sub-pipeline operation.
/// </summary>
/// <remarks>
/// Creates a new async sub-pipeline operation.
/// </remarks>
/// <param name="name">Optional name for this scope.</param>
/// <param name="logger">Optional logger for diagnostics.</param>
public class SubPipelineOperationAsync(string? name = null, ILogger<SubPipelineOperationAsync>? logger = null) : IOperationAsync
{
    private readonly List<IOperationAsync> _operations = [];
    private readonly List<IOperationAsync> _executedOperations = [];
    private readonly ILogger<SubPipelineOperationAsync>? _logger = logger;

    /// <summary>
    /// Gets the name of this sub-pipeline scope.
    /// </summary>
    public string? Name { get; } = name;

    /// <inheritdoc />
    public bool IsRequired => false;

    /// <summary>
    /// Gets the operations in this sub-pipeline.
    /// </summary>
    public IReadOnlyList<IOperationAsync> Operations => _operations.AsReadOnly();

    /// <summary>
    /// Adds an operation to this sub-pipeline.
    /// </summary>
    /// <param name="operation">The operation to add.</param>
    /// <returns>This sub-pipeline for chaining.</returns>
    public SubPipelineOperationAsync Add(IOperationAsync operation)
    {
        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        _operations.Add(operation);
        return this;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(IPipelineMessage input)
    {
        await ExecuteAsync(input, CancellationToken.None);
    }

    /// <summary>
    /// Executes the sub-pipeline with cancellation support.
    /// </summary>
    public async Task ExecuteAsync(IPipelineMessage input, CancellationToken cancellationToken)
    {
        string scopeName = Name ?? "anonymous";
        _logger?.LogDebug("SubPipelineOperationAsync: ExecuteAsync started. Scope: {ScopeName}, Operations: {OperationCount}", scopeName, _operations.Count);
        _executedOperations.Clear();

        try
        {
            foreach (IOperationAsync operation in _operations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (input.IsLocked && !operation.IsRequired)
                {
                    continue;
                }

                _logger?.LogDebug("SubPipelineOperationAsync: Operation '{OperationName}' started. Scope: {ScopeName}", operation.GetType().Name, scopeName);
                try
                {
                    if (operation is IOperationAsyncWithCancellation operationWithCancellation)
                    {
                        await operationWithCancellation.ExecuteAsync(input, cancellationToken);
                    }
                    else
                    {
                        await operation.ExecuteAsync(input);
                    }
                    _executedOperations.Add(operation);
                }
                finally
                {
                    _logger?.LogDebug("SubPipelineOperationAsync: Operation '{OperationName}' finished. Scope: {ScopeName}", operation.GetType().Name, scopeName);
                }

                if (input.IsFaulty)
                {
                    break;
                }
            }
        }
        finally
        {
            _logger?.LogDebug("SubPipelineOperationAsync: ExecuteAsync finished. Scope: {ScopeName}", scopeName);
        }
    }

    /// <inheritdoc />
    public async Task RollbackAsync(IPipelineMessage input)
    {
        foreach (IOperationAsync operation in _executedOperations.Reverse<IOperationAsync>())
        {
            try
            {
                await operation.RollbackAsync(input);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "SubPipelineOperationAsync: RollbackAsync failed");
            }
        }
    }
}

