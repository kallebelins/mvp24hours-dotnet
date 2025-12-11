//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Infrastructure.Cqrs.Behaviors;
using Mvp24Hours.Infrastructure.Cqrs.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Observability;
using Xunit;

namespace Mvp24Hours.Infrastructure.Cqrs.Test;

/// <summary>
/// Tests for observability components: RequestContext, Tracing, Telemetry, and Audit.
/// </summary>
public class ObservabilityTest
{
    #region [ RequestContext Tests ]

    [Fact]
    public void RequestContext_ShouldGenerateNewIds_WhenCreatedWithDefaults()
    {
        // Arrange & Act
        var context = new RequestContext();

        // Assert
        Assert.NotNull(context.CorrelationId);
        Assert.NotNull(context.RequestId);
        Assert.NotEqual(context.CorrelationId, context.RequestId);
        Assert.Null(context.CausationId);
        Assert.True(context.Timestamp <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void RequestContext_ShouldUseProvidedValues_WhenCreatedWithParameters()
    {
        // Arrange
        var correlationId = "correlation-123";
        var causationId = "causation-456";
        var userId = "user-789";
        var tenantId = "tenant-abc";

        // Act
        var context = new RequestContext(
            correlationId: correlationId,
            causationId: causationId,
            userId: userId,
            tenantId: tenantId);

        // Assert
        Assert.Equal(correlationId, context.CorrelationId);
        Assert.Equal(causationId, context.CausationId);
        Assert.Equal(userId, context.UserId);
        Assert.Equal(tenantId, context.TenantId);
    }

    [Fact]
    public void RequestContext_CreateChildContext_ShouldInheritCorrelationId()
    {
        // Arrange
        var parentContext = new RequestContext(
            correlationId: "parent-correlation",
            userId: "user-123",
            tenantId: "tenant-abc");

        // Act
        var childContext = parentContext.CreateChildContext();

        // Assert
        Assert.Equal(parentContext.CorrelationId, childContext.CorrelationId);
        Assert.Equal(parentContext.RequestId, childContext.CausationId);
        Assert.NotEqual(parentContext.RequestId, childContext.RequestId);
        Assert.Equal(parentContext.UserId, childContext.UserId);
        Assert.Equal(parentContext.TenantId, childContext.TenantId);
    }

    [Fact]
    public void RequestContext_FromCorrelationId_ShouldCreateContextWithProvidedId()
    {
        // Arrange
        var correlationId = "external-correlation-id";

        // Act
        var context = RequestContext.FromCorrelationId(correlationId, userId: "user-1");

        // Assert
        Assert.Equal(correlationId, context.CorrelationId);
        Assert.Null(context.CausationId);
        Assert.Equal("user-1", context.UserId);
    }

    [Fact]
    public void RequestContextAccessor_ShouldMaintainContextAcrossAsyncOperations()
    {
        // Arrange
        var accessor = new RequestContextAccessor();
        var context = new RequestContext(correlationId: "test-correlation");

        // Act
        accessor.Context = context;

        // Assert
        Assert.Same(context, accessor.Context);

        // Clean up
        accessor.Context = null;
        Assert.Null(accessor.Context);
    }

    [Fact]
    public void RequestContextFactory_ShouldCreateContextWithProvidedValues()
    {
        // Arrange
        var factory = new RequestContextFactory();
        var metadata = new Dictionary<string, object?> { { "key", "value" } };

        // Act
        var context = factory.Create(
            correlationId: "factory-correlation",
            causationId: "factory-causation",
            userId: "factory-user",
            tenantId: "factory-tenant",
            metadata: metadata);

        // Assert
        Assert.Equal("factory-correlation", context.CorrelationId);
        Assert.Equal("factory-causation", context.CausationId);
        Assert.Equal("factory-user", context.UserId);
        Assert.Equal("factory-tenant", context.TenantId);
        Assert.Equal("value", context.Metadata["key"]);
    }

    #endregion

    #region [ RequestContextBehavior Tests ]

    [Fact]
    public async Task RequestContextBehavior_ShouldEstablishContextForRequest()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssemblyContaining<ObservabilityTest>();
            options.RegisterRequestContextBehavior = true;
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<ISender>();
        var contextAccessor = provider.GetRequiredService<IRequestContextAccessor>();

        // Act
        var result = await mediator.SendAsync(new GetContextCommand());

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.CorrelationId);
        Assert.NotNull(result.RequestId);
    }

    #endregion

    #region [ Audit Tests ]

    [Fact]
    public void AuditEntry_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var entry = new AuditEntry
        {
            OperationName = "TestOperation",
            OperationType = "Command"
        };

        // Assert
        Assert.NotNull(entry.Id);
        Assert.Equal("TestOperation", entry.OperationName);
        Assert.Equal("Command", entry.OperationType);
        Assert.True(entry.Timestamp <= DateTimeOffset.UtcNow);
        Assert.False(entry.IsSuccess);
    }

    [Fact]
    public async Task InMemoryAuditStore_ShouldSaveAndRetrieveEntries()
    {
        // Arrange
        var store = new InMemoryAuditStore();
        var entry = new AuditEntry
        {
            OperationName = "CreateUser",
            OperationType = "Command",
            UserId = "user-123",
            CorrelationId = "correlation-456",
            IsSuccess = true
        };

        // Act
        await store.SaveAsync(entry);

        // Assert
        Assert.Single(store.Entries);
        Assert.Equal("CreateUser", store.Entries[0].OperationName);
    }

    [Fact]
    public async Task InMemoryAuditStore_ShouldFilterByCorrelationId()
    {
        // Arrange
        var store = new InMemoryAuditStore();
        await store.SaveAsync(new AuditEntry
        {
            OperationName = "Op1",
            OperationType = "Command",
            CorrelationId = "corr-1"
        });
        await store.SaveAsync(new AuditEntry
        {
            OperationName = "Op2",
            OperationType = "Query",
            CorrelationId = "corr-2"
        });
        await store.SaveAsync(new AuditEntry
        {
            OperationName = "Op3",
            OperationType = "Command",
            CorrelationId = "corr-1"
        });

        // Act
        var entries = store.GetByCorrelationId("corr-1");

        // Assert
        Assert.Equal(2, entries.Count);
        Assert.All(entries, e => Assert.Equal("corr-1", e.CorrelationId));
    }

    [Fact]
    public async Task InMemoryAuditStore_ShouldFilterByUserId()
    {
        // Arrange
        var store = new InMemoryAuditStore();
        await store.SaveAsync(new AuditEntry
        {
            OperationName = "Op1",
            OperationType = "Command",
            UserId = "user-1"
        });
        await store.SaveAsync(new AuditEntry
        {
            OperationName = "Op2",
            OperationType = "Query",
            UserId = "user-2"
        });

        // Act
        var entries = store.GetByUserId("user-1");

        // Assert
        Assert.Single(entries);
        Assert.Equal("user-1", entries[0].UserId);
    }

    [Fact]
    public async Task InMemoryAuditStore_SaveBatchAsync_ShouldSaveMultipleEntries()
    {
        // Arrange
        var store = new InMemoryAuditStore();
        var entries = new[]
        {
            new AuditEntry { OperationName = "Op1", OperationType = "Command" },
            new AuditEntry { OperationName = "Op2", OperationType = "Query" },
            new AuditEntry { OperationName = "Op3", OperationType = "Command" }
        };

        // Act
        await store.SaveBatchAsync(entries);

        // Assert
        Assert.Equal(3, store.Entries.Count);
    }

    [Fact]
    public async Task InMemoryAuditStore_Clear_ShouldRemoveAllEntries()
    {
        // Arrange
        var store = new InMemoryAuditStore();
        await store.SaveAsync(new AuditEntry { OperationName = "Op1", OperationType = "Command" });
        await store.SaveAsync(new AuditEntry { OperationName = "Op2", OperationType = "Query" });

        // Act
        store.Clear();

        // Assert
        Assert.Empty(store.Entries);
    }

    #endregion

    #region [ OpenTelemetry Integration Tests ]

    [Fact]
    public void MediatorActivitySource_ShouldHaveCorrectSourceName()
    {
        // Assert
        Assert.Equal("Mvp24Hours.Mediator", MediatorActivitySource.SourceName);
        Assert.NotNull(MediatorActivitySource.Source);
    }

    [Fact]
    public void MediatorActivitySource_ActivityNames_ShouldHaveCorrectValues()
    {
        // Assert
        Assert.Equal("Mvp24Hours.Mediator.Request", MediatorActivitySource.ActivityNames.Request);
        Assert.Equal("Mvp24Hours.Mediator.Command", MediatorActivitySource.ActivityNames.Command);
        Assert.Equal("Mvp24Hours.Mediator.Query", MediatorActivitySource.ActivityNames.Query);
        Assert.Equal("Mvp24Hours.Mediator.Notification", MediatorActivitySource.ActivityNames.Notification);
        Assert.Equal("Mvp24Hours.Mediator.DomainEvent", MediatorActivitySource.ActivityNames.DomainEvent);
    }

    [Fact]
    public void MediatorActivitySource_TagNames_ShouldFollowOpenTelemetryConventions()
    {
        // Assert
        Assert.Equal("mediator.request.name", MediatorActivitySource.TagNames.RequestName);
        Assert.Equal("mediator.correlation_id", MediatorActivitySource.TagNames.CorrelationId);
        Assert.Equal("enduser.id", MediatorActivitySource.TagNames.UserId); // OpenTelemetry semantic convention
        Assert.Equal("error.type", MediatorActivitySource.TagNames.ErrorType);
    }

    [Fact]
    public void ActivityExtensions_WithContext_ShouldSetTags()
    {
        // Arrange
        var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var context = new RequestContext(
            correlationId: "test-correlation",
            userId: "test-user",
            tenantId: "test-tenant");

        // Act
        using var activity = MediatorActivitySource.Source.StartActivity("Test");
        if (activity != null)
        {
            activity.WithContext(context);

            // Assert
            Assert.Equal("test-correlation", activity.GetTagItem(MediatorActivitySource.TagNames.CorrelationId));
            Assert.Equal("test-user", activity.GetTagItem(MediatorActivitySource.TagNames.UserId));
            Assert.Equal("test-tenant", activity.GetTagItem(MediatorActivitySource.TagNames.TenantId));
        }

        listener.Dispose();
    }

    #endregion

    #region [ Telemetry Event Names Tests ]

    [Fact]
    public void TelemetryEventNames_ShouldHaveCorrectValues()
    {
        // Assert
        Assert.Equal("mediator-request-start", TelemetryEventNames.MediatorRequestStart);
        Assert.Equal("mediator-request-success", TelemetryEventNames.MediatorRequestSuccess);
        Assert.Equal("mediator-request-failure", TelemetryEventNames.MediatorRequestFailure);
        Assert.Equal("domain-event-raised", TelemetryEventNames.DomainEventRaised);
        Assert.Equal("notification-published", TelemetryEventNames.NotificationPublished);
        Assert.Equal("integration-event-sent", TelemetryEventNames.IntegrationEventSent);
        Assert.Equal("audit-entry-created", TelemetryEventNames.AuditEntryCreated);
    }

    #endregion

    #region [ Integration Tests with DI ]

    [Fact]
    public async Task Mediator_WithObservabilityBehaviors_ShouldWorkCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssemblyContaining<ObservabilityTest>();
            options.WithObservabilityBehaviors();
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<ISender>();

        // Act
        var result = await mediator.SendAsync(new GetContextCommand());

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Mediator_WithAuditBehavior_ShouldCreateAuditEntries()
    {
        // Arrange
        var auditStore = new InMemoryAuditStore();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAuditStore>(auditStore);
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssemblyContaining<ObservabilityTest>();
            options.WithAuditBehavior();
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<ISender>();

        // Act
        await mediator.SendAsync(new AuditableCommand { Value = "test" });

        // Assert
        Assert.Single(auditStore.Entries);
        Assert.Equal(nameof(AuditableCommand), auditStore.Entries[0].OperationName);
        Assert.True(auditStore.Entries[0].IsSuccess);
    }

    [Fact]
    public async Task Mediator_WithAuditBehavior_ShouldRecordFailures()
    {
        // Arrange
        var auditStore = new InMemoryAuditStore();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAuditStore>(auditStore);
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssemblyContaining<ObservabilityTest>();
            options.WithAuditBehavior();
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<ISender>();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.SendAsync(new FailingAuditableCommand()));

        Assert.Single(auditStore.Entries);
        Assert.False(auditStore.Entries[0].IsSuccess);
        Assert.Contains("InvalidOperationException", auditStore.Entries[0].ErrorType);
    }

    #endregion

    #region [ Test Commands and Handlers ]

    public record GetContextCommand : IMediatorCommand<ContextResult>;
    
    public record ContextResult(string? CorrelationId, string? RequestId, string? CausationId);

    public class GetContextCommandHandler : IMediatorCommandHandler<GetContextCommand, ContextResult>
    {
        private readonly IRequestContextAccessor _contextAccessor;

        public GetContextCommandHandler(IRequestContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public Task<ContextResult> Handle(GetContextCommand request, CancellationToken cancellationToken)
        {
            var context = _contextAccessor.Context;
            return Task.FromResult(new ContextResult(
                context?.CorrelationId,
                context?.RequestId,
                context?.CausationId));
        }
    }

    public record AuditableCommand : IMediatorCommand, IAuditable
    {
        public string Value { get; init; } = string.Empty;
        public bool ShouldAuditRequestData => true;
    }

    public class AuditableCommandHandler : IMediatorRequestHandler<AuditableCommand, Unit>
    {
        public Task<Unit> Handle(AuditableCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Unit.Value);
        }
    }

    public record FailingAuditableCommand : IMediatorCommand, IAuditable;

    public class FailingAuditableCommandHandler : IMediatorRequestHandler<FailingAuditableCommand, Unit>
    {
        public Task<Unit> Handle(FailingAuditableCommand request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Test failure for audit");
        }
    }

    #endregion
}

