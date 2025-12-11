# API Reference

## Main Interfaces

### IMediator

Main interface that combines all functionalities.

```csharp
public interface IMediator : ISender, IPublisher, IStreamSender
{
}
```

### ISender

Sends requests (commands/queries) to handlers.

```csharp
public interface ISender
{
    Task<TResponse> SendAsync<TResponse>(
        IMediatorRequest<TResponse> request,
        CancellationToken cancellationToken = default);
}
```

### IPublisher

Publishes notifications to multiple handlers.

```csharp
public interface IPublisher
{
    Task PublishAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : IMediatorNotification;
    
    Task PublishAsync<TNotification>(
        TNotification notification,
        NotificationPublishingStrategy strategy,
        CancellationToken cancellationToken = default)
        where TNotification : IMediatorNotification;
}
```

### IStreamSender

Sends requests that return async streams.

```csharp
public interface IStreamSender
{
    IAsyncEnumerable<TResponse> CreateStreamAsync<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default);
}
```

## Request Abstractions

### IMediatorRequest\<TResponse\>

```csharp
public interface IMediatorRequest<TResponse>
{
}
```

### IMediatorRequest

```csharp
public interface IMediatorRequest : IMediatorRequest<Unit>
{
}
```

### IMediatorCommand\<TResponse\>

```csharp
public interface IMediatorCommand<TResponse> : IMediatorRequest<TResponse>
{
}
```

### IMediatorCommand

```csharp
public interface IMediatorCommand : IMediatorCommand<Unit>
{
}
```

### IMediatorQuery\<TResponse\>

```csharp
public interface IMediatorQuery<TResponse> : IMediatorRequest<TResponse>
{
}
```

### IStreamRequest\<TResponse\>

```csharp
public interface IStreamRequest<TResponse>
{
}
```

## Handlers

### IMediatorRequestHandler\<TRequest, TResponse\>

```csharp
public interface IMediatorRequestHandler<in TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
```

### IMediatorCommandHandler\<TCommand, TResponse\>

```csharp
public interface IMediatorCommandHandler<in TCommand, TResponse>
    : IMediatorRequestHandler<TCommand, TResponse>
    where TCommand : IMediatorCommand<TResponse>
{
}
```

### IMediatorCommandHandler\<TCommand\>

```csharp
public interface IMediatorCommandHandler<in TCommand>
    : IMediatorCommandHandler<TCommand, Unit>
    where TCommand : IMediatorCommand
{
}
```

### IMediatorQueryHandler\<TQuery, TResponse\>

```csharp
public interface IMediatorQueryHandler<in TQuery, TResponse>
    : IMediatorRequestHandler<TQuery, TResponse>
    where TQuery : IMediatorQuery<TResponse>
{
}
```

### IStreamRequestHandler\<TRequest, TResponse\>

```csharp
public interface IStreamRequestHandler<in TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
```

## Notifications

### IMediatorNotification

```csharp
public interface IMediatorNotification
{
}
```

### IMediatorNotificationHandler\<TNotification\>

```csharp
public interface IMediatorNotificationHandler<in TNotification>
    where TNotification : IMediatorNotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
```

### NotificationPublishingStrategy

```csharp
public enum NotificationPublishingStrategy
{
    Sequential,
    Parallel,
    ParallelNoWait
}
```

## Pipeline Behaviors

### IPipelineBehavior\<TRequest, TResponse\>

```csharp
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}
```

### RequestHandlerDelegate\<TResponse\>

```csharp
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
```

## Marker Interfaces

### ITransactional

```csharp
public interface ITransactional
{
}
```

### ICacheableRequest

```csharp
public interface ICacheableRequest
{
    string CacheKey { get; }
    TimeSpan? AbsoluteExpiration { get; }
    TimeSpan? SlidingExpiration { get; }
}
```

### ICacheInvalidator

```csharp
public interface ICacheInvalidator
{
    IEnumerable<string> CacheKeysToInvalidate { get; }
}
```

### IAuthorized

```csharp
public interface IAuthorized
{
    IEnumerable<string> RequiredRoles { get; }
    IEnumerable<string> RequiredPolicies { get; }
}
```

### IRetryable

```csharp
public interface IRetryable
{
    int MaxRetryAttempts { get; }
    int BaseDelayMilliseconds { get; }
}
```

### IIdempotentCommand

```csharp
public interface IIdempotentCommand
{
    string? IdempotencyKey { get; }
}
```

## Domain Events

### IDomainEvent

```csharp
public interface IDomainEvent : IMediatorNotification
{
}
```

### IDomainEventHandler\<TEvent\>

```csharp
public interface IDomainEventHandler<in TEvent> : IMediatorNotificationHandler<TEvent>
    where TEvent : IDomainEvent
{
}
```

### IHasDomainEvents

```csharp
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void AddDomainEvent(IDomainEvent eventItem);
    void RemoveDomainEvent(IDomainEvent eventItem);
    void ClearDomainEvents();
}
```

### IDomainEventDispatcher

```csharp
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default);
}
```

## Integration Events

### IIntegrationEvent

```csharp
public interface IIntegrationEvent
{
    Guid Id { get; }
    DateTime OccurredOn { get; }
    string? CorrelationId { get; }
}
```

### IIntegrationEventHandler\<TEvent\>

```csharp
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
```

### IIntegrationEventOutbox

```csharp
public interface IIntegrationEventOutbox
{
    Task AddAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize = 100, CancellationToken cancellationToken = default);
    Task MarkAsPublishedAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);
}
```

### IIntegrationEventPublisher

```csharp
public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}
```

## DI Extensions

### AddMvpMediator

```csharp
public static IServiceCollection AddMvpMediator(
    this IServiceCollection services,
    Action<MediatorOptions>? configure = null);
```

### MediatorOptions

```csharp
public sealed class MediatorOptions
{
    public bool RegisterLoggingBehavior { get; set; }
    public bool RegisterPerformanceBehavior { get; set; }
    public bool RegisterUnhandledExceptionBehavior { get; set; }
    public bool RegisterValidationBehavior { get; set; }
    public bool RegisterCachingBehavior { get; set; }
    public bool RegisterTransactionBehavior { get; set; }
    public bool RegisterAuthorizationBehavior { get; set; }
    public bool RegisterRetryBehavior { get; set; }
    public bool RegisterIdempotencyBehavior { get; set; }
    
    public int PerformanceThresholdMilliseconds { get; set; } = 500;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryBaseDelayMilliseconds { get; set; } = 100;
    public int IdempotencyDurationHours { get; set; } = 24;
    
    public NotificationPublishingStrategy DefaultNotificationStrategy { get; set; }
    
    public void RegisterHandlersFromAssembly(Assembly assembly);
    public void RegisterHandlersFromAssemblyContaining<T>();
    
    public MediatorOptions WithAllBehaviors();
    public MediatorOptions WithSecurityBehaviors();
    public MediatorOptions WithResiliencyBehaviors();
}
```

### Cache Extensions

```csharp
public static IServiceCollection AddMediatorMemoryCache(
    this IServiceCollection services,
    Action<MediatorCacheOptions>? configure = null);

public static IServiceCollection AddMediatorRedisCache(
    this IServiceCollection services,
    string connectionString,
    string? instanceName = null,
    Action<MediatorCacheOptions>? configure = null);
```

## Unit

Struct to represent absence of value.

```csharp
public readonly struct Unit : IEquatable<Unit>
{
    public static readonly Unit Value = new();
    
    public bool Equals(Unit other) => true;
    public override bool Equals(object? obj) => obj is Unit;
    public override int GetHashCode() => 0;
    public override string ToString() => "()";
    
    public static bool operator ==(Unit left, Unit right) => true;
    public static bool operator !=(Unit left, Unit right) => false;
}
```

