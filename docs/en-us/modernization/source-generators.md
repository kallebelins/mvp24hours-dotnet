# Source Generators

## Overview

Source generators are a C# compiler feature that allows generating code at compile time, eliminating reflection-based approaches and enabling:

- **Better Performance**: No runtime reflection overhead
- **Native AOT Compatibility**: Full support for ahead-of-time compilation
- **Smaller Application Size**: Trimming-friendly code
- **Faster Startup**: No JIT compilation for generated code
- **Compile-Time Validation**: Errors detected at build time

## Key Source Generator Features in Mvp24Hours

### 1. JSON Serialization with `[JsonSerializable]`

The framework provides source-generated JSON serialization contexts for AOT-friendly serialization:

```csharp
// Use the built-in context
var json = JsonSerializer.Serialize(result, Mvp24HoursJsonSerializerContext.Default.BusinessResultOfString);

// Or with the default options
var json = JsonSerializer.Serialize(result, Mvp24HoursJsonSerializerContext.DefaultOptions);
```

#### Creating Custom Contexts

```csharp
// For your domain types
[JsonSerializable(typeof(Order))]
[JsonSerializable(typeof(Customer))]
[JsonSerializable(typeof(List<OrderItem>))]
public partial class DomainJsonContext : JsonSerializerContext { }

// Register in DI
services.AddMvp24HoursJsonSourceGeneration()
        .AddJsonSerializerContext(DomainJsonContext.Default);
```

#### MVC Integration

```csharp
services.AddControllers()
        .AddMvp24HoursJsonSourceGeneration();

// With custom contexts
services.AddControllers()
        .AddMvp24HoursJsonSourceGeneration(DomainJsonContext.Default);
```

### 2. High-Performance Logging with `[LoggerMessage]`

The framework provides source-generated logger messages for zero-allocation logging:

```csharp
// Instead of string interpolation (allocates memory):
_logger.LogInformation($"Operation {operationName} completed in {elapsed}ms");

// Use source-generated methods (zero allocation):
CoreLoggerMessages.OperationCompleted(_logger, operationName, elapsed);
```

#### Available Logger Message Classes

| Module | Class | Event ID Range |
|--------|-------|----------------|
| Core | `CoreLoggerMessages` | 1000-1999 |
| Pipeline | `PipelineLoggerMessages` | 2000-2999 |
| CQRS | `CqrsLoggerMessages` | 3000-3999 |
| RabbitMQ | `RabbitMQLoggerMessages` | 4000-4999 |
| EFCore | `EFCoreLoggerMessages` | 5000-5999 |
| WebAPI | `WebAPILoggerMessages` | 6000-6999 |

#### Usage Examples

```csharp
using Mvp24Hours.Core.Logging;

public class MyService
{
    private readonly ILogger<MyService> _logger;

    public async Task ProcessOrderAsync(Order order)
    {
        var sw = Stopwatch.StartNew();
        
        CoreLoggerMessages.OperationStarted(_logger, "ProcessOrder", order.Id.ToString());
        
        try
        {
            // Process order...
            
            CoreLoggerMessages.OperationCompleted(_logger, "ProcessOrder", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            CoreLoggerMessages.OperationFailed(_logger, ex, "ProcessOrder", sw.ElapsedMilliseconds);
            throw;
        }
    }
}
```

### 3. Available JsonSerializerContexts

| Context | Module | Types Included |
|---------|--------|----------------|
| `Mvp24HoursJsonSerializerContext` | Core | BusinessResult, MessageResult, PagingResult, VoidResult |
| `CqrsJsonSerializerContext` | CQRS | DomainEventBase, IntegrationEventBase, SagaState, ScheduledCommand |
| `RabbitMQJsonSerializerContext` | RabbitMQ | MessageEnvelope, Response<T>, ScheduledMessage |

## Native AOT Compatibility

### Checking AOT Mode

```csharp
using Mvp24Hours.Core.Serialization.SourceGeneration;

if (AotCompatibility.IsNativeAot)
{
    // Running in Native AOT mode
}
```

### AOT-Compatible Methods

Methods marked with `[AotCompatible]` are safe to use in Native AOT:

```csharp
[AotCompatible]
public void SafeMethod()
{
    // Uses source-generated code only
}
```

### Methods Requiring Reflection

Methods marked with `[RequiresReflection]` may not work in Native AOT:

```csharp
[RequiresReflection("Uses Activator.CreateInstance", Alternative = "Use factory pattern")]
public T CreateInstance<T>()
{
    return Activator.CreateInstance<T>();
}
```

## Configuration

### Enabling Source Generation

```csharp
// In Program.cs or Startup.cs
services.AddMvp24HoursJsonSourceGeneration();

// With custom configuration
services.AddMvp24HoursJsonSourceGeneration(options =>
{
    options.WriteIndented = true;
    options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});
```

### Combining Multiple Contexts

```csharp
services.AddMvp24HoursJsonSourceGeneration()
        .AddJsonSerializerContext(CqrsJsonSerializerContext.Default)
        .AddJsonSerializerContext(RabbitMQJsonSerializerContext.Default)
        .AddJsonSerializerContext(DomainJsonContext.Default);
```

## Performance Comparison

| Approach | Allocation | Startup Time | AOT Compatible |
|----------|------------|--------------|----------------|
| Reflection-based JSON | ~5KB/operation | Slow | ❌ |
| Source-generated JSON | ~0 | Fast | ✅ |
| String interpolation logging | ~200B/log | - | ✅ |
| LoggerMessage logging | ~0 | - | ✅ |

## Best Practices

1. **Always use source-generated JSON** for types that are serialized frequently
2. **Use LoggerMessage** for hot paths and high-frequency logging
3. **Create domain-specific contexts** for your application types
4. **Mark AOT-incompatible code** with appropriate attributes
5. **Test with Native AOT** before deploying to production

## Migration Guide

### From Newtonsoft.Json

```csharp
// Before (Newtonsoft.Json)
var json = JsonConvert.SerializeObject(result, JsonHelper.JsonDefaultSettings);

// After (Source-generated)
var json = JsonSerializer.Serialize(result, Mvp24HoursJsonSerializerContext.DefaultOptions);
```

### From ILogger String Interpolation

```csharp
// Before (allocates)
_logger.LogInformation($"Command {commandName} completed in {elapsed}ms");

// After (zero allocation)
CqrsLoggerMessages.CommandCompleted(_logger, commandName, elapsed, true);
```

## Related Documentation

- [.NET 9 Features](dotnet9-features.md)
- [Migration Guide](migration-guide.md)
- [Observability](../observability/home.md)

