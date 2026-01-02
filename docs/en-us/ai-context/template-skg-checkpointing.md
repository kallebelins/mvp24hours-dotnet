# Checkpointing and Recovery Template - Semantic Kernel Graph

> **Purpose**: This template provides AI agents with patterns for implementing fault-tolerant workflows with checkpointing and recovery using Semantic Kernel Graph.

---

## Overview

Checkpointing enables saving execution state at specific points, allowing recovery and resumption from failures. This template covers:
- State serialization and persistence
- Checkpoint creation and restoration
- Version management and migration
- Recovery policies and strategies
- Cleanup and retention policies

---

## When to Use This Template

| Scenario | Recommendation |
|----------|----------------|
| Long-running workflows | ✅ Recommended |
| Fault-tolerant systems | ✅ Recommended |
| Expensive operations | ✅ Recommended |
| Distributed processing | ✅ Recommended |
| Simple short workflows | ⚠️ May add overhead |
| Real-time processing | ⚠️ Consider performance impact |

---

## Required NuGet Packages

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.SemanticKernel" Version="1.*" />
  <PackageReference Include="SemanticKernel.Graph" Version="1.*" />
  <PackageReference Include="Microsoft.Extensions.Logging" Version="8.*" />
  <PackageReference Include="System.Text.Json" Version="8.*" />
</ItemGroup>
```

---

## Checkpointing Architecture

```
┌────────────────────────────────────────────────────────────┐
│                  Checkpointing System                       │
├────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────┐     ┌──────────────────┐                 │
│  │   Workflow   │────▶│   Node Execute   │                 │
│  └──────────────┘     └────────┬─────────┘                 │
│                                │                            │
│                    ┌───────────▼───────────┐               │
│                    │  Should Checkpoint?   │               │
│                    │  - Interval           │               │
│                    │  - Critical Node      │               │
│                    │  - Time-based         │               │
│                    └───────────┬───────────┘               │
│                                │ Yes                        │
│                    ┌───────────▼───────────┐               │
│                    │  StateHelpers         │               │
│                    │  - Serialize          │               │
│                    │  - Compress           │               │
│                    │  - Validate           │               │
│                    └───────────┬───────────┘               │
│                                │                            │
│                    ┌───────────▼───────────┐               │
│                    │  CheckpointManager    │               │
│                    │  - Store              │               │
│                    │  - Retrieve           │               │
│                    │  - Cleanup            │               │
│                    └───────────────────────┘               │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │                 Recovery Service                      │  │
│  │  - Detect Failure  →  Find Checkpoint  →  Restore    │  │
│  └──────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────┘
```

---

## Core Components

### Configuration Models

```csharp
/// <summary>
/// Options for serializing state to checkpoints.
/// </summary>
public class SerializationOptions
{
    /// <summary>
    /// Enable compression for storage efficiency.
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// Only compress if data exceeds this size (bytes).
    /// </summary>
    public int CompressionThreshold { get; set; } = 1024;

    /// <summary>
    /// Include metadata in serialization.
    /// </summary>
    public bool IncludeMetadata { get; set; } = true;

    /// <summary>
    /// Include execution history in serialization.
    /// </summary>
    public bool IncludeExecutionHistory { get; set; } = false;

    /// <summary>
    /// Validate integrity before serialization.
    /// </summary>
    public bool ValidateIntegrity { get; set; } = true;

    /// <summary>
    /// Use indented JSON format (debugging).
    /// </summary>
    public bool Indented { get; set; } = false;
}

/// <summary>
/// Options for checkpointing behavior.
/// </summary>
public class CheckpointingOptions
{
    /// <summary>
    /// Create checkpoint every N nodes.
    /// </summary>
    public int CheckpointInterval { get; set; } = 5;

    /// <summary>
    /// Create checkpoint after time interval.
    /// </summary>
    public TimeSpan? CheckpointTimeInterval { get; set; }

    /// <summary>
    /// Create checkpoint at start of execution.
    /// </summary>
    public bool CreateInitialCheckpoint { get; set; } = true;

    /// <summary>
    /// Create checkpoint at end of execution.
    /// </summary>
    public bool CreateFinalCheckpoint { get; set; } = true;

    /// <summary>
    /// Create checkpoint when errors occur.
    /// </summary>
    public bool CreateErrorCheckpoints { get; set; } = true;

    /// <summary>
    /// Nodes that always trigger checkpoints.
    /// </summary>
    public HashSet<string> CriticalNodes { get; set; } = new();

    /// <summary>
    /// Enable automatic cleanup of old checkpoints.
    /// </summary>
    public bool EnableAutoCleanup { get; set; } = true;

    /// <summary>
    /// Stop execution if checkpointing fails.
    /// </summary>
    public bool FailOnCheckpointError { get; set; } = false;

    /// <summary>
    /// Retention policy for checkpoints.
    /// </summary>
    public CheckpointRetentionPolicy RetentionPolicy { get; set; } = new();
}

/// <summary>
/// Policy for retaining checkpoints.
/// </summary>
public class CheckpointRetentionPolicy
{
    /// <summary>
    /// Maximum age of checkpoints.
    /// </summary>
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Maximum checkpoints per execution.
    /// </summary>
    public int MaxCheckpointsPerExecution { get; set; } = 100;

    /// <summary>
    /// Maximum total storage in bytes.
    /// </summary>
    public long MaxTotalStorageBytes { get; set; } = 100 * 1024 * 1024; // 100MB

    /// <summary>
    /// Always keep critical checkpoints regardless of policy.
    /// </summary>
    public bool KeepCriticalCheckpoints { get; set; } = true;
}

/// <summary>
/// Options for automatic recovery.
/// </summary>
public class RecoveryOptions
{
    /// <summary>
    /// Maximum number of recovery attempts.
    /// </summary>
    public int MaxRecoveryAttempts { get; set; } = 3;

    /// <summary>
    /// Enable automatic recovery on failure.
    /// </summary>
    public bool EnableAutomaticRecovery { get; set; } = true;

    /// <summary>
    /// Maximum time for recovery operation.
    /// </summary>
    public TimeSpan RecoveryTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Preferred recovery strategy.
    /// </summary>
    public RecoveryStrategy PreferredRecoveryStrategy { get; set; } = RecoveryStrategy.CheckpointRestore;
}

public enum RecoveryStrategy
{
    CheckpointRestore,
    Retry,
    Skip,
    Fallback
}
```

---

## Implementation Patterns

### 1. Checkpoint Data Models

```csharp
using System.Text.Json;
using System.IO.Compression;
using System.Security.Cryptography;

/// <summary>
/// Represents a saved checkpoint.
/// </summary>
public class Checkpoint
{
    public string CheckpointId { get; set; } = Guid.NewGuid().ToString();
    public string ExecutionId { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public int SequenceNumber { get; set; }
    public byte[] StateData { get; set; } = Array.Empty<byte>();
    public string Checksum { get; set; } = string.Empty;
    public bool IsCompressed { get; set; }
    public long SizeInBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public bool IsCritical { get; set; }
}

/// <summary>
/// Result of a checkpoint validation.
/// </summary>
public class CheckpointValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public long SizeInBytes { get; set; }
    public bool IsCompressed { get; set; }
    public string? Checksum { get; set; }
}

/// <summary>
/// Result of a recovery operation.
/// </summary>
public class RecoveryResult
{
    public bool IsSuccessful { get; set; }
    public RecoveryStrategy RecoveryStrategy { get; set; }
    public TimeSpan RecoveryDuration { get; set; }
    public string? Reason { get; set; }
    public string? CheckpointId { get; set; }
}
```

### 2. State Helpers

```csharp
/// <summary>
/// Utilities for state serialization and checkpointing.
/// </summary>
public static class StateHelpers
{
    private static readonly JsonSerializerOptions _defaultOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Serializes state to JSON bytes.
    /// </summary>
    public static byte[] SerializeState(Dictionary<string, object?> state, SerializationOptions? options = null)
    {
        options ??= new SerializationOptions();

        // Create serializable state
        var serializableState = new Dictionary<string, object?>
        {
            ["values"] = state,
            ["version"] = "1.0.0",
            ["timestamp"] = DateTime.UtcNow
        };

        if (options.IncludeMetadata)
        {
            serializableState["metadata"] = new
            {
                serializedAt = DateTime.UtcNow,
                includesHistory = options.IncludeExecutionHistory
            };
        }

        var jsonOptions = options.Indented
            ? new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            : _defaultOptions;

        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(serializableState, jsonOptions);

        // Compress if enabled and above threshold
        if (options.EnableCompression && jsonBytes.Length > options.CompressionThreshold)
        {
            return Compress(jsonBytes);
        }

        return jsonBytes;
    }

    /// <summary>
    /// Deserializes state from bytes.
    /// </summary>
    public static Dictionary<string, object?> DeserializeState(byte[] data)
    {
        // Try to decompress
        var jsonBytes = TryDecompress(data) ?? data;

        var deserialized = JsonSerializer.Deserialize<Dictionary<string, object?>>(jsonBytes, _defaultOptions);
        
        if (deserialized?.TryGetValue("values", out var values) == true && values is JsonElement element)
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(element.GetRawText(), _defaultOptions) 
                   ?? new Dictionary<string, object?>();
        }

        return deserialized ?? new Dictionary<string, object?>();
    }

    /// <summary>
    /// Creates a checksum for integrity validation.
    /// </summary>
    public static string CreateChecksum(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Validates checksum.
    /// </summary>
    public static bool ValidateChecksum(byte[] data, string expectedChecksum)
    {
        var actualChecksum = CreateChecksum(data);
        return actualChecksum == expectedChecksum;
    }

    private static byte[] Compress(byte[] data)
    {
        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal))
        {
            gzipStream.Write(data, 0, data.Length);
        }
        return outputStream.ToArray();
    }

    private static byte[]? TryDecompress(byte[] data)
    {
        try
        {
            using var inputStream = new MemoryStream(data);
            using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
            using var outputStream = new MemoryStream();
            gzipStream.CopyTo(outputStream);
            return outputStream.ToArray();
        }
        catch
        {
            return null; // Not compressed
        }
    }
}
```

### 3. Checkpoint Manager

```csharp
/// <summary>
/// Interface for checkpoint management.
/// </summary>
public interface ICheckpointManager
{
    Task<Checkpoint> CreateCheckpointAsync(
        string executionId,
        string nodeId,
        Dictionary<string, object?> state,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    Task<Checkpoint?> GetCheckpointAsync(string checkpointId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Checkpoint>> ListCheckpointsAsync(string executionId, int limit = 100, CancellationToken cancellationToken = default);
    Task<CheckpointValidationResult> ValidateCheckpointAsync(string checkpointId, CancellationToken cancellationToken = default);
    Task DeleteCheckpointAsync(string checkpointId, CancellationToken cancellationToken = default);
    Task CleanupOldCheckpointsAsync(CheckpointRetentionPolicy policy, CancellationToken cancellationToken = default);
    Task<CheckpointStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of checkpoint manager.
/// </summary>
public class InMemoryCheckpointManager : ICheckpointManager
{
    private readonly Dictionary<string, Checkpoint> _checkpoints = new();
    private readonly SerializationOptions _serializationOptions;
    private int _sequenceCounter;

    public InMemoryCheckpointManager(SerializationOptions? serializationOptions = null)
    {
        _serializationOptions = serializationOptions ?? new SerializationOptions();
    }

    public Task<Checkpoint> CreateCheckpointAsync(
        string executionId,
        string nodeId,
        Dictionary<string, object?> state,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var stateData = StateHelpers.SerializeState(state, _serializationOptions);
        var isCompressed = _serializationOptions.EnableCompression && 
                          stateData.Length > _serializationOptions.CompressionThreshold;

        var checkpoint = new Checkpoint
        {
            ExecutionId = executionId,
            NodeId = nodeId,
            SequenceNumber = Interlocked.Increment(ref _sequenceCounter),
            StateData = stateData,
            Checksum = StateHelpers.CreateChecksum(stateData),
            IsCompressed = isCompressed,
            SizeInBytes = stateData.Length,
            Metadata = metadata ?? new Dictionary<string, object>()
        };

        _checkpoints[checkpoint.CheckpointId] = checkpoint;

        return Task.FromResult(checkpoint);
    }

    public Task<Checkpoint?> GetCheckpointAsync(string checkpointId, CancellationToken cancellationToken = default)
    {
        _checkpoints.TryGetValue(checkpointId, out var checkpoint);
        return Task.FromResult(checkpoint);
    }

    public Task<IReadOnlyList<Checkpoint>> ListCheckpointsAsync(
        string executionId, 
        int limit = 100, 
        CancellationToken cancellationToken = default)
    {
        var checkpoints = _checkpoints.Values
            .Where(c => c.ExecutionId == executionId)
            .OrderByDescending(c => c.SequenceNumber)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<Checkpoint>>(checkpoints);
    }

    public Task<CheckpointValidationResult> ValidateCheckpointAsync(
        string checkpointId, 
        CancellationToken cancellationToken = default)
    {
        if (!_checkpoints.TryGetValue(checkpointId, out var checkpoint))
        {
            return Task.FromResult(new CheckpointValidationResult
            {
                IsValid = false,
                ErrorMessage = "Checkpoint not found"
            });
        }

        var isValid = StateHelpers.ValidateChecksum(checkpoint.StateData, checkpoint.Checksum);

        return Task.FromResult(new CheckpointValidationResult
        {
            IsValid = isValid,
            ErrorMessage = isValid ? null : "Checksum mismatch",
            SizeInBytes = checkpoint.SizeInBytes,
            IsCompressed = checkpoint.IsCompressed,
            Checksum = checkpoint.Checksum
        });
    }

    public Task DeleteCheckpointAsync(string checkpointId, CancellationToken cancellationToken = default)
    {
        _checkpoints.Remove(checkpointId);
        return Task.CompletedTask;
    }

    public Task CleanupOldCheckpointsAsync(
        CheckpointRetentionPolicy policy, 
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow - policy.MaxAge;
        var toRemove = _checkpoints
            .Where(kvp => kvp.Value.CreatedAt < cutoffDate && 
                         (!policy.KeepCriticalCheckpoints || !kvp.Value.IsCritical))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var id in toRemove)
        {
            _checkpoints.Remove(id);
        }

        return Task.CompletedTask;
    }

    public Task<CheckpointStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var checkpoints = _checkpoints.Values.ToList();
        
        return Task.FromResult(new CheckpointStatistics
        {
            TotalCheckpoints = checkpoints.Count,
            TotalStorageBytes = checkpoints.Sum(c => c.SizeInBytes),
            AverageCheckpointSizeBytes = checkpoints.Count > 0 
                ? (long)checkpoints.Average(c => c.SizeInBytes) 
                : 0,
            OldestCheckpoint = checkpoints.MinBy(c => c.CreatedAt)?.CreatedAt,
            NewestCheckpoint = checkpoints.MaxBy(c => c.CreatedAt)?.CreatedAt
        });
    }
}

public class CheckpointStatistics
{
    public int TotalCheckpoints { get; set; }
    public long TotalStorageBytes { get; set; }
    public long AverageCheckpointSizeBytes { get; set; }
    public double AverageCompressionRatio { get; set; }
    public double CacheHitRate { get; set; }
    public DateTime? OldestCheckpoint { get; set; }
    public DateTime? NewestCheckpoint { get; set; }
}
```

### 4. Checkpointing Executor

```csharp
using Microsoft.SemanticKernel;
using SemanticKernel.Graph.Core;

/// <summary>
/// Graph executor with checkpointing support.
/// </summary>
public class CheckpointingGraphExecutor
{
    private readonly GraphExecutor _executor;
    private readonly ICheckpointManager _checkpointManager;
    private readonly CheckpointingOptions _options;
    private readonly ILogger _logger;

    private int _nodeCount;
    private DateTime _lastCheckpointTime;

    public string? LastExecutionId { get; private set; }

    public CheckpointingGraphExecutor(
        GraphExecutor executor,
        ICheckpointManager checkpointManager,
        CheckpointingOptions options,
        ILogger logger)
    {
        _executor = executor;
        _checkpointManager = checkpointManager;
        _options = options;
        _logger = logger;
        _lastCheckpointTime = DateTime.UtcNow;
    }

    public async Task<KernelArguments> ExecuteAsync(
        Kernel kernel,
        KernelArguments arguments,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        LastExecutionId = executionId;
        arguments["_executionId"] = executionId;

        // Create initial checkpoint
        if (_options.CreateInitialCheckpoint)
        {
            await CreateCheckpointAsync(executionId, "start", arguments, true);
        }

        try
        {
            // Execute with checkpointing hooks
            await _executor.ExecuteAsync(kernel, arguments, cancellationToken);

            // Create final checkpoint
            if (_options.CreateFinalCheckpoint)
            {
                await CreateCheckpointAsync(executionId, "end", arguments, true);
            }

            return arguments;
        }
        catch (Exception ex)
        {
            // Create error checkpoint
            if (_options.CreateErrorCheckpoints)
            {
                arguments["_error"] = ex.Message;
                arguments["_errorType"] = ex.GetType().Name;
                await CreateCheckpointAsync(executionId, "error", arguments, true);
            }

            throw;
        }
    }

    public async Task OnNodeExecutedAsync(
        string executionId,
        string nodeId,
        KernelArguments arguments)
    {
        _nodeCount++;

        var shouldCheckpoint = ShouldCreateCheckpoint(nodeId);

        if (shouldCheckpoint)
        {
            await CreateCheckpointAsync(executionId, nodeId, arguments);
        }
    }

    private bool ShouldCreateCheckpoint(string nodeId)
    {
        // Critical node
        if (_options.CriticalNodes.Contains(nodeId))
            return true;

        // Interval-based
        if (_nodeCount % _options.CheckpointInterval == 0)
            return true;

        // Time-based
        if (_options.CheckpointTimeInterval.HasValue)
        {
            var elapsed = DateTime.UtcNow - _lastCheckpointTime;
            if (elapsed >= _options.CheckpointTimeInterval.Value)
                return true;
        }

        return false;
    }

    private async Task CreateCheckpointAsync(
        string executionId,
        string nodeId,
        KernelArguments arguments,
        bool isCritical = false)
    {
        try
        {
            var state = arguments.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            
            var checkpoint = await _checkpointManager.CreateCheckpointAsync(
                executionId,
                nodeId,
                state,
                new Dictionary<string, object>
                {
                    ["nodeCount"] = _nodeCount,
                    ["isCritical"] = isCritical
                });

            _lastCheckpointTime = DateTime.UtcNow;
            _logger.LogDebug("Created checkpoint {CheckpointId} at node {NodeId}", 
                checkpoint.CheckpointId, nodeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create checkpoint at node {NodeId}", nodeId);
            
            if (_options.FailOnCheckpointError)
                throw;
        }
    }

    public async Task<IReadOnlyList<Checkpoint>> GetExecutionCheckpointsAsync(string executionId)
    {
        return await _checkpointManager.ListCheckpointsAsync(executionId);
    }

    public async Task<KernelArguments> ResumeFromCheckpointAsync(
        string checkpointId,
        Kernel kernel,
        CancellationToken cancellationToken = default)
    {
        var checkpoint = await _checkpointManager.GetCheckpointAsync(checkpointId, cancellationToken);
        
        if (checkpoint == null)
            throw new InvalidOperationException($"Checkpoint {checkpointId} not found");

        // Validate checkpoint
        var validation = await _checkpointManager.ValidateCheckpointAsync(checkpointId, cancellationToken);
        if (!validation.IsValid)
            throw new InvalidOperationException($"Checkpoint validation failed: {validation.ErrorMessage}");

        // Restore state
        var state = StateHelpers.DeserializeState(checkpoint.StateData);
        var arguments = new KernelArguments();
        
        foreach (var kvp in state)
        {
            arguments[kvp.Key] = kvp.Value;
        }

        _logger.LogInformation("Resuming from checkpoint {CheckpointId} at node {NodeId}", 
            checkpointId, checkpoint.NodeId);

        // Continue execution from checkpoint node
        return await ExecuteAsync(kernel, arguments, cancellationToken);
    }
}
```

### 5. Recovery Service

```csharp
/// <summary>
/// Service for automatic recovery from failures.
/// </summary>
public class GraphRecoveryService
{
    private readonly ICheckpointManager _checkpointManager;
    private readonly ILogger<GraphRecoveryService> _logger;

    public GraphRecoveryService(
        ICheckpointManager checkpointManager,
        ILogger<GraphRecoveryService> logger)
    {
        _checkpointManager = checkpointManager;
        _logger = logger;
    }

    public async Task<RecoveryResult> AttemptRecoveryAsync(
        FailureContext failureContext,
        Kernel kernel,
        RecoveryOptions options,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        if (!options.EnableAutomaticRecovery)
        {
            return new RecoveryResult
            {
                IsSuccessful = false,
                Reason = "Automatic recovery is disabled"
            };
        }

        for (int attempt = 1; attempt <= options.MaxRecoveryAttempts; attempt++)
        {
            _logger.LogInformation("Recovery attempt {Attempt} of {MaxAttempts}", 
                attempt, options.MaxRecoveryAttempts);

            try
            {
                var result = await TryRecoveryStrategyAsync(
                    failureContext, 
                    kernel, 
                    options.PreferredRecoveryStrategy,
                    cancellationToken);

                if (result.IsSuccessful)
                {
                    result.RecoveryDuration = DateTime.UtcNow - startTime;
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Recovery attempt {Attempt} failed", attempt);
            }

            // Wait before next attempt
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
        }

        return new RecoveryResult
        {
            IsSuccessful = false,
            Reason = $"All {options.MaxRecoveryAttempts} recovery attempts failed",
            RecoveryDuration = DateTime.UtcNow - startTime
        };
    }

    private async Task<RecoveryResult> TryRecoveryStrategyAsync(
        FailureContext failureContext,
        Kernel kernel,
        RecoveryStrategy strategy,
        CancellationToken cancellationToken)
    {
        return strategy switch
        {
            RecoveryStrategy.CheckpointRestore => await RestoreFromCheckpointAsync(failureContext, cancellationToken),
            RecoveryStrategy.Retry => await RetryExecutionAsync(failureContext, kernel, cancellationToken),
            RecoveryStrategy.Skip => SkipFailedNode(failureContext),
            RecoveryStrategy.Fallback => await ExecuteFallbackAsync(failureContext, kernel, cancellationToken),
            _ => new RecoveryResult { IsSuccessful = false, Reason = "Unknown strategy" }
        };
    }

    private async Task<RecoveryResult> RestoreFromCheckpointAsync(
        FailureContext failureContext,
        CancellationToken cancellationToken)
    {
        var checkpoints = await _checkpointManager.ListCheckpointsAsync(
            failureContext.ExecutionId, 
            limit: 10, 
            cancellationToken);

        var latestValid = checkpoints.FirstOrDefault();

        if (latestValid == null)
        {
            return new RecoveryResult
            {
                IsSuccessful = false,
                Reason = "No valid checkpoints found"
            };
        }

        // Validate checkpoint
        var validation = await _checkpointManager.ValidateCheckpointAsync(
            latestValid.CheckpointId, 
            cancellationToken);

        if (!validation.IsValid)
        {
            return new RecoveryResult
            {
                IsSuccessful = false,
                Reason = $"Checkpoint validation failed: {validation.ErrorMessage}"
            };
        }

        return new RecoveryResult
        {
            IsSuccessful = true,
            RecoveryStrategy = RecoveryStrategy.CheckpointRestore,
            CheckpointId = latestValid.CheckpointId
        };
    }

    private Task<RecoveryResult> RetryExecutionAsync(
        FailureContext failureContext,
        Kernel kernel,
        CancellationToken cancellationToken)
    {
        // Retry from beginning
        return Task.FromResult(new RecoveryResult
        {
            IsSuccessful = true,
            RecoveryStrategy = RecoveryStrategy.Retry
        });
    }

    private RecoveryResult SkipFailedNode(FailureContext failureContext)
    {
        return new RecoveryResult
        {
            IsSuccessful = true,
            RecoveryStrategy = RecoveryStrategy.Skip
        };
    }

    private Task<RecoveryResult> ExecuteFallbackAsync(
        FailureContext failureContext,
        Kernel kernel,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new RecoveryResult
        {
            IsSuccessful = true,
            RecoveryStrategy = RecoveryStrategy.Fallback
        });
    }
}

public class FailureContext
{
    public string ExecutionId { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public KernelArguments? Arguments { get; set; }
    public DateTime FailureTime { get; set; } = DateTime.UtcNow;
}
```

---

## Service Layer Integration

```csharp
public interface ICheckpointingService
{
    Task<string> ExecuteWithCheckpointingAsync(KernelArguments arguments, CancellationToken cancellationToken = default);
    Task<string> ResumeFromCheckpointAsync(string checkpointId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Checkpoint>> GetCheckpointsAsync(string executionId, CancellationToken cancellationToken = default);
    Task<CheckpointStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

public class CheckpointingService : ICheckpointingService
{
    private readonly Kernel _kernel;
    private readonly CheckpointingGraphExecutor _executor;
    private readonly ICheckpointManager _checkpointManager;
    private readonly GraphRecoveryService _recoveryService;
    private readonly ILogger<CheckpointingService> _logger;

    public CheckpointingService(
        Kernel kernel,
        ILoggerFactory loggerFactory)
    {
        _kernel = kernel;
        _logger = loggerFactory.CreateLogger<CheckpointingService>();
        
        _checkpointManager = new InMemoryCheckpointManager();
        
        var graphExecutor = new GraphExecutor("CheckpointedWorkflow", "Workflow with checkpointing");
        var options = new CheckpointingOptions
        {
            CheckpointInterval = 3,
            CreateInitialCheckpoint = true,
            CreateFinalCheckpoint = true,
            CreateErrorCheckpoints = true
        };
        
        _executor = new CheckpointingGraphExecutor(
            graphExecutor, 
            _checkpointManager, 
            options,
            _logger);

        _recoveryService = new GraphRecoveryService(
            _checkpointManager,
            loggerFactory.CreateLogger<GraphRecoveryService>());
    }

    public async Task<string> ExecuteWithCheckpointingAsync(
        KernelArguments arguments,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _executor.ExecuteAsync(_kernel, arguments, cancellationToken);
            return _executor.LastExecutionId ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Execution failed, attempting recovery");

            var failureContext = new FailureContext
            {
                ExecutionId = _executor.LastExecutionId ?? string.Empty,
                Exception = ex,
                Arguments = arguments
            };

            var recovery = await _recoveryService.AttemptRecoveryAsync(
                failureContext,
                _kernel,
                new RecoveryOptions(),
                cancellationToken);

            if (recovery.IsSuccessful && !string.IsNullOrEmpty(recovery.CheckpointId))
            {
                return await ResumeFromCheckpointAsync(recovery.CheckpointId, cancellationToken);
            }

            throw;
        }
    }

    public async Task<string> ResumeFromCheckpointAsync(
        string checkpointId,
        CancellationToken cancellationToken = default)
    {
        var result = await _executor.ResumeFromCheckpointAsync(checkpointId, _kernel, cancellationToken);
        return _executor.LastExecutionId ?? string.Empty;
    }

    public Task<IReadOnlyList<Checkpoint>> GetCheckpointsAsync(
        string executionId,
        CancellationToken cancellationToken = default)
    {
        return _checkpointManager.ListCheckpointsAsync(executionId, cancellationToken: cancellationToken);
    }

    public Task<CheckpointStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return _checkpointManager.GetStatisticsAsync(cancellationToken);
    }
}
```

---

## Dependency Injection Setup

```csharp
public static class CheckpointingServiceExtensions
{
    public static IServiceCollection AddCheckpointingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register Kernel
        services.AddSingleton(sp =>
        {
            var builder = Kernel.CreateBuilder();
            builder.AddGraphSupport();
            builder.AddOpenAIChatCompletion(
                modelId: configuration["AI:ModelId"] ?? "gpt-4o",
                apiKey: configuration["AI:ApiKey"]!);
            return builder.Build();
        });

        // Register checkpoint manager
        services.AddSingleton<ICheckpointManager>(sp =>
        {
            var options = new SerializationOptions
            {
                EnableCompression = true,
                CompressionThreshold = 1024
            };
            return new InMemoryCheckpointManager(options);
        });

        // Register checkpointing service
        services.AddScoped<ICheckpointingService, CheckpointingService>();

        return services;
    }
}
```

---

## Web API Integration

```csharp
[ApiController]
[Route("api/[controller]")]
public class CheckpointsController : ControllerBase
{
    private readonly ICheckpointingService _service;
    private readonly ILogger<CheckpointsController> _logger;

    public CheckpointsController(ICheckpointingService service, ILogger<CheckpointsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteWithCheckpointing(
        [FromBody] ExecuteRequest request,
        CancellationToken cancellationToken)
    {
        var arguments = new KernelArguments();
        foreach (var kvp in request.Parameters)
        {
            arguments[kvp.Key] = kvp.Value;
        }

        var executionId = await _service.ExecuteWithCheckpointingAsync(arguments, cancellationToken);
        return Ok(new { executionId });
    }

    [HttpPost("resume/{checkpointId}")]
    public async Task<IActionResult> ResumeFromCheckpoint(
        string checkpointId,
        CancellationToken cancellationToken)
    {
        var executionId = await _service.ResumeFromCheckpointAsync(checkpointId, cancellationToken);
        return Ok(new { executionId });
    }

    [HttpGet("{executionId}/checkpoints")]
    public async Task<IActionResult> GetCheckpoints(
        string executionId,
        CancellationToken cancellationToken)
    {
        var checkpoints = await _service.GetCheckpointsAsync(executionId, cancellationToken);
        return Ok(checkpoints.Select(c => new
        {
            c.CheckpointId,
            c.NodeId,
            c.SequenceNumber,
            c.SizeInBytes,
            c.IsCompressed,
            c.CreatedAt,
            c.IsCritical
        }));
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics(CancellationToken cancellationToken)
    {
        var stats = await _service.GetStatisticsAsync(cancellationToken);
        return Ok(stats);
    }
}

public record ExecuteRequest(Dictionary<string, string> Parameters);
```

---

## Testing

```csharp
using Xunit;

public class CheckpointingTests
{
    [Fact]
    public async Task CheckpointManager_CreatesAndRestores_Successfully()
    {
        // Arrange
        var manager = new InMemoryCheckpointManager();
        var state = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42
        };

        // Act
        var checkpoint = await manager.CreateCheckpointAsync("exec-1", "node-1", state);
        var retrieved = await manager.GetCheckpointAsync(checkpoint.CheckpointId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("exec-1", retrieved.ExecutionId);
        Assert.Equal("node-1", retrieved.NodeId);
    }

    [Fact]
    public async Task CheckpointManager_Validates_Integrity()
    {
        // Arrange
        var manager = new InMemoryCheckpointManager();
        var state = new Dictionary<string, object?> { ["key"] = "value" };
        var checkpoint = await manager.CreateCheckpointAsync("exec-1", "node-1", state);

        // Act
        var validation = await manager.ValidateCheckpointAsync(checkpoint.CheckpointId);

        // Assert
        Assert.True(validation.IsValid);
        Assert.NotNull(validation.Checksum);
    }

    [Fact]
    public void StateHelpers_SerializesAndDeserializes()
    {
        // Arrange
        var state = new Dictionary<string, object?>
        {
            ["string"] = "test",
            ["number"] = 123,
            ["boolean"] = true
        };

        // Act
        var serialized = StateHelpers.SerializeState(state);
        var deserialized = StateHelpers.DeserializeState(serialized);

        // Assert
        Assert.Equal("test", deserialized["string"]?.ToString());
    }

    [Fact]
    public void StateHelpers_CompressesLargeState()
    {
        // Arrange
        var largeState = new Dictionary<string, object?>
        {
            ["data"] = new string('x', 2000) // > 1KB threshold
        };
        var options = new SerializationOptions
        {
            EnableCompression = true,
            CompressionThreshold = 1024
        };

        // Act
        var compressed = StateHelpers.SerializeState(largeState, options);
        var uncompressed = StateHelpers.SerializeState(largeState, new SerializationOptions { EnableCompression = false });

        // Assert
        Assert.True(compressed.Length < uncompressed.Length);
    }

    [Fact]
    public async Task RecoveryService_FindsLatestCheckpoint()
    {
        // Arrange
        var manager = new InMemoryCheckpointManager();
        var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var recoveryService = new GraphRecoveryService(manager, loggerFactory.CreateLogger<GraphRecoveryService>());

        await manager.CreateCheckpointAsync("exec-1", "node-1", new Dictionary<string, object?>());
        await manager.CreateCheckpointAsync("exec-1", "node-2", new Dictionary<string, object?>());

        var failureContext = new FailureContext { ExecutionId = "exec-1" };

        // Act
        var result = await recoveryService.AttemptRecoveryAsync(
            failureContext,
            null!,
            new RecoveryOptions { PreferredRecoveryStrategy = RecoveryStrategy.CheckpointRestore },
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.CheckpointId);
    }
}
```

---

## Best Practices

### Checkpoint Design

1. **Strategic Placement**: Checkpoint after expensive or critical operations
2. **Size Management**: Monitor and compress large states
3. **Validation**: Always validate before restore
4. **Critical Nodes**: Mark important nodes for mandatory checkpoints

### Performance

1. **Compression**: Enable for states larger than 1KB
2. **Selective Serialization**: Exclude unnecessary data
3. **Async Operations**: Checkpoint asynchronously when possible
4. **Caching**: Cache frequently accessed checkpoints

### Recovery

1. **Multiple Points**: Maintain multiple recovery points
2. **Validation**: Validate restored state consistency
3. **Timeout Handling**: Configure appropriate recovery timeouts
4. **Logging**: Log all recovery attempts and outcomes

### Retention

1. **Age-based Cleanup**: Remove old checkpoints automatically
2. **Size Limits**: Set maximum storage limits
3. **Critical Preservation**: Keep critical checkpoints regardless of age
4. **Audit Trail**: Maintain audit logs for compliance

---

## Related Templates

- [Graph Executor](template-skg-graph-executor.md) - Basic graph execution
- [Multi-Agent](template-skg-multi-agent.md) - Coordinated agents
- [Human-in-the-Loop](template-skg-human-in-loop.md) - Human intervention
- [Streaming](template-skg-streaming.md) - Real-time events

---

## External References

- [Semantic Kernel Graph](https://github.com/kallebelins/semantic-kernel-graph)
- [State Management Patterns](https://docs.microsoft.com/azure/architecture/patterns/category/data-management)
- [Fault Tolerance Patterns](https://docs.microsoft.com/azure/architecture/patterns/category/resiliency)

