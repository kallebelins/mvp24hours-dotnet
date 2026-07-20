//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Data;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using CoreDomainEvent = Mvp24Hours.Core.Contract.Domain.Entity.IDomainEvent;
using CoreHasDomainEvents = Mvp24Hours.Core.Contract.Domain.Entity.IHasDomainEvents;

namespace Mvp24Hours.Infrastructure.Cqrs.Test.Support;

/// <summary>
/// Captures log entries for assertions in pipeline behavior tests.
/// </summary>
public sealed class CollectingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add((logLevel, formatter(state, exception), exception));
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}

/// <summary>
/// Sync mock of <see cref="IUnitOfWork"/> for DomainEventExtensions tests.
/// </summary>
public sealed class MockUnitOfWork : IUnitOfWork
{
    public List<string> OperationsLog { get; } = [];
    public int SaveChangesCallCount { get; private set; }
    public bool ShouldThrowOnSave { get; set; }
    public Exception? ExceptionToThrow { get; set; }
    public int RowsAffected { get; set; } = 1;

    public int SaveChanges(CancellationToken cancellationToken = default)
    {
        if (ShouldThrowOnSave)
        {
            throw ExceptionToThrow ?? new InvalidOperationException("SaveChanges failed");
        }

        SaveChangesCallCount++;
        OperationsLog.Add("SaveChanges");
        return RowsAffected;
    }

    public void Rollback() => OperationsLog.Add("Rollback");

    public IRepository<T> GetRepository<T>() where T : class, IEntityBase =>
        throw new NotImplementedException();

    public IDbConnection GetConnection() => throw new NotImplementedException();

    public void Dispose() => OperationsLog.Add("Dispose");
}

/// <summary>
/// Publisher that always fails — used to test DomainEventDispatcher error paths.
/// </summary>
public sealed class ThrowingPublisher : IPublisher
{
    public string Message { get; init; } = "Publish failed";

    public Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : IMediatorNotification
        => throw new InvalidOperationException(Message);
}

/// <summary>
/// Publisher that records published notifications.
/// </summary>
public sealed class RecordingPublisher : IPublisher
{
    public List<object> Published { get; } = [];

    public Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : IMediatorNotification
    {
        Published.Add(notification!);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Stream request without a registered handler.
/// </summary>
public sealed class UnhandledStreamRequest : IStreamRequest<int>
{
    public int Count { get; set; } = 1;
}

/// <summary>
/// Domain event used only for failure-path dispatcher tests.
/// </summary>
public record FailingDispatchEvent : MediatorDomainEventBase
{
    public string Reason { get; init; } = "fail";
}

/// <summary>
/// Aggregate that can raise <see cref="FailingDispatchEvent"/>.
/// </summary>
public sealed class FailingTestAggregate : CoreHasDomainEvents
{
    private readonly List<CoreDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<CoreDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    public void RaiseFailingEvent(string reason = "fail") =>
        _domainEvents.Add(new FailingDispatchEvent { Reason = reason });
}

/// <summary>
/// Extra validator that always fails for Age &lt; 18 — multi-validator aggregation.
/// </summary>
public sealed class CreateUserMustBeAdultValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserMustBeAdultValidator()
    {
        RuleFor(x => x.Age)
            .GreaterThanOrEqualTo(18)
            .WithMessage("User must be at least 18");
    }
}

/// <summary>
/// Validator that emits a failure with null PropertyName to exercise ErrorCode fallback.
/// </summary>
public sealed class CreateUserNullPropertyValidator : IValidator<CreateUserCommand>
{
    public bool CanValidateInstancesOfType(Type type) => type == typeof(CreateUserCommand);

    public IValidatorDescriptor CreateDescriptor() => throw new NotSupportedException();

    public ValidationResult Validate(IValidationContext context) =>
        Validate((ValidationContext<CreateUserCommand>)context);

    public Task<ValidationResult> ValidateAsync(IValidationContext context, CancellationToken cancellation = default) =>
        ValidateAsync((ValidationContext<CreateUserCommand>)context, cancellation);

    public ValidationResult Validate(CreateUserCommand instance) =>
        new([new ValidationFailure(null!, "Custom error") { ErrorCode = "CUSTOM_CODE" }]);

    public Task<ValidationResult> ValidateAsync(CreateUserCommand instance, CancellationToken cancellation = default) =>
        Task.FromResult(Validate(instance));

    public ValidationResult Validate(ValidationContext<CreateUserCommand> context) =>
        Validate(context.InstanceToValidate);

    public Task<ValidationResult> ValidateAsync(ValidationContext<CreateUserCommand> context, CancellationToken cancellation = default) =>
        Task.FromResult(Validate(context.InstanceToValidate));
}

/// <summary>
/// Integration event paired with UserRegisteredEvent for conversion tests.
/// </summary>
public sealed record UserRegisteredIntegrationEvent : IntegrationEventBase
{
    public int UserId { get; init; }
    public string Email { get; init; } = string.Empty;
}

/// <summary>
/// Converts UserRegisteredEvent to UserRegisteredIntegrationEvent.
/// </summary>
public sealed class UserRegisteredToIntegrationConverter
    : IDomainToIntegrationEventConverter<UserRegisteredEvent, UserRegisteredIntegrationEvent>
{
    public UserRegisteredIntegrationEvent? Convert(UserRegisteredEvent domainEvent) =>
        new()
        {
            UserId = domainEvent.UserId,
            Email = domainEvent.Email,
            CorrelationId = domainEvent.EventId.ToString()
        };
}

/// <summary>
/// Stub inbox store for UseInboxStore replacement tests.
/// </summary>
public sealed class StubInboxStore : IInboxStore
{
    public Task<bool> ExistsAsync(Guid messageId, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task MarkAsProcessedAsync(Guid messageId, string messageType, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<InboxMessage?> GetByIdAsync(Guid messageId, CancellationToken cancellationToken = default) =>
        Task.FromResult<InboxMessage?>(null);

    public Task<IReadOnlyList<InboxMessage>> GetByTimeRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<InboxMessage>>([]);

    public Task<int> CleanupAsync(DateTime olderThan, CancellationToken cancellationToken = default) =>
        Task.FromResult(0);
}

/// <summary>
/// Stub outbox for UseOutboxStore replacement tests.
/// </summary>
public sealed class StubOutboxStore : IIntegrationEventOutbox
{
    public List<IIntegrationEvent> Events { get; } = [];

    public Task AddAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        Events.Add(@event);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize = 100, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OutboxMessage>>([]);

    public Task MarkAsPublishedAsync(Guid messageId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<int> CleanupAsync(DateTime olderThan, CancellationToken cancellationToken = default) =>
        Task.FromResult(0);
}

/// <summary>
/// Stub dead-letter store for UseDeadLetterStore replacement tests.
/// </summary>
public sealed class StubDeadLetterStore : IDeadLetterStore
{
    public Task AddAsync(DeadLetterMessage message, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyList<DeadLetterMessage>> GetAllAsync(int limit = 100, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DeadLetterMessage>>([]);

    public Task<IReadOnlyList<DeadLetterMessage>> GetByEventTypeAsync(string eventType, int limit = 100, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DeadLetterMessage>>([]);

    public Task<DeadLetterMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult<DeadLetterMessage?>(null);

    public Task<bool> RequeueAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task MarkAsResolvedAsync(Guid id, string resolution, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<int> GetCountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(0);
}

/// <summary>
/// Stub integration event publisher for UseIntegrationEventPublisher tests.
/// </summary>
public sealed class StubIntegrationEventPublisher : IIntegrationEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent => Task.CompletedTask;

    public Task PublishFromOutboxAsync(OutboxMessage message, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
