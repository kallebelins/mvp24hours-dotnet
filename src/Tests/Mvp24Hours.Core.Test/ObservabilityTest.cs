using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Observability;
using System.Diagnostics;
using Xunit;

namespace Mvp24Hours.Core.Test;

/// <summary>
/// Tests for OpenTelemetry Tracing components in Core.Observability
/// </summary>
public class ObservabilityTest
{
    #region ActivitySources Tests

    [Fact]
    public void ActivitySources_AllSourceNames_ShouldContainAllModules()
    {
        // Arrange & Act
        var allSources = Mvp24HoursActivitySources.AllSourceNames;

        // Assert
        allSources.Should().NotBeEmpty();
        allSources.Should().Contain(Mvp24HoursActivitySources.Core.Name);
        allSources.Should().Contain(Mvp24HoursActivitySources.Pipe.Name);
        allSources.Should().Contain(Mvp24HoursActivitySources.Cqrs.Name);
        allSources.Should().Contain(Mvp24HoursActivitySources.Data.Name);
        allSources.Should().Contain(Mvp24HoursActivitySources.RabbitMQ.Name);
        allSources.Should().Contain(Mvp24HoursActivitySources.WebAPI.Name);
        allSources.Should().Contain(Mvp24HoursActivitySources.Caching.Name);
        allSources.Should().Contain(Mvp24HoursActivitySources.CronJob.Name);
        allSources.Should().Contain(Mvp24HoursActivitySources.Infrastructure.Name);
    }

    [Fact]
    public void ActivitySources_CoreSource_ShouldBeValid()
    {
        // Arrange & Act
        var source = Mvp24HoursActivitySources.Core.Source;

        // Assert
        source.Should().NotBeNull();
        source.Name.Should().Be("Mvp24Hours.Core");
        source.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void ActivitySources_ModuleSources_ShouldHaveCorrectNames()
    {
        // Assert
        Mvp24HoursActivitySources.Core.Name.Should().Be("Mvp24Hours.Core");
        Mvp24HoursActivitySources.Pipe.Name.Should().Be("Mvp24Hours.Pipe");
        Mvp24HoursActivitySources.Cqrs.Name.Should().Be("Mvp24Hours.Cqrs");
        Mvp24HoursActivitySources.Data.Name.Should().Be("Mvp24Hours.Data");
        Mvp24HoursActivitySources.RabbitMQ.Name.Should().Be("Mvp24Hours.RabbitMQ");
        Mvp24HoursActivitySources.WebAPI.Name.Should().Be("Mvp24Hours.WebAPI");
        Mvp24HoursActivitySources.Caching.Name.Should().Be("Mvp24Hours.Caching");
        Mvp24HoursActivitySources.CronJob.Name.Should().Be("Mvp24Hours.CronJob");
        Mvp24HoursActivitySources.Infrastructure.Name.Should().Be("Mvp24Hours.Infrastructure");
    }

    #endregion

    #region ActivityHelper Tests

    [Fact]
    public void ActivityHelper_StartOperation_ShouldCreateActivity()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        using var activity = ActivityHelper.StartOperation(
            Mvp24HoursActivitySources.Core.Source,
            "TestOperation",
            "Test");

        // Assert
        activity.Should().NotBeNull();
        activity!.DisplayName.Should().Be("TestOperation");
        activity.GetTagItem(SemanticTags.OperationName).Should().Be("TestOperation");
        activity.GetTagItem(SemanticTags.OperationType).Should().Be("Test");
    }

    [Fact]
    public void ActivityHelper_StartCommandActivity_ShouldSetCorrectTags()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        using var activity = ActivityHelper.StartCommandActivity("CreateOrder");

        // Assert
        activity.Should().NotBeNull();
        activity!.GetTagItem(SemanticTags.MediatorRequestName).Should().Be("CreateOrder");
        activity.GetTagItem(SemanticTags.MediatorRequestType).Should().Be("Command");
    }

    [Fact]
    public void ActivityHelper_StartQueryActivity_ShouldSetCorrectTags()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        using var activity = ActivityHelper.StartQueryActivity("GetOrderById");

        // Assert
        activity.Should().NotBeNull();
        activity!.GetTagItem(SemanticTags.MediatorRequestName).Should().Be("GetOrderById");
        activity.GetTagItem(SemanticTags.MediatorRequestType).Should().Be("Query");
    }

    [Fact]
    public void ActivityHelper_StartPipelineActivity_ShouldSetCorrectTags()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        using var activity = ActivityHelper.StartPipelineActivity("OrderPipeline", 5);

        // Assert
        activity.Should().NotBeNull();
        activity!.GetTagItem(SemanticTags.PipelineName).Should().Be("OrderPipeline");
        activity.GetTagItem(SemanticTags.PipelineTotalOperations).Should().Be(5);
    }

    [Fact]
    public void ActivityHelper_StartDatabaseActivity_ShouldSetCorrectTags()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        using var activity = ActivityHelper.StartDatabaseActivity(
            "GetOrder",
            "SELECT",
            "sqlserver",
            "OrdersDb");

        // Assert
        activity.Should().NotBeNull();
        activity!.GetTagItem(SemanticTags.DbOperation).Should().Be("SELECT");
        activity.GetTagItem(SemanticTags.DbSystem).Should().Be("sqlserver");
        activity.GetTagItem(SemanticTags.DbName).Should().Be("OrdersDb");
    }

    [Fact]
    public void ActivityHelper_StartCacheActivity_ShouldSetCorrectTags()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        using var activity = ActivityHelper.StartCacheActivity("GET", "order:123", "redis");

        // Assert
        activity.Should().NotBeNull();
        activity!.GetTagItem(SemanticTags.CacheOperation).Should().Be("GET");
        activity.GetTagItem(SemanticTags.CacheKey).Should().Be("order:123");
        activity.GetTagItem(SemanticTags.CacheSystem).Should().Be("redis");
    }

    [Fact]
    public void ActivityHelper_GetTracePropagationHeaders_ShouldReturnW3CHeaders()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = Mvp24HoursActivitySources.Core.Source.StartActivity("Test");

        // Act
        var headers = ActivityHelper.GetTracePropagationHeaders();

        // Assert
        headers.Should().ContainKey("traceparent");
        headers["traceparent"].Should().StartWith("00-");
    }

    #endregion

    #region ActivityExtensions Tests

    [Fact]
    public void ActivityExtensions_SetSuccess_ShouldSetCorrectStatus()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = Mvp24HoursActivitySources.Core.Source.StartActivity("Test");

        // Act
        activity.SetSuccess();

        // Assert
        activity.Should().NotBeNull();
        activity!.Status.Should().Be(ActivityStatusCode.Ok);
        activity.GetTagItem(SemanticTags.OperationSuccess).Should().Be(true);
    }

    [Fact]
    public void ActivityExtensions_SetError_ShouldSetCorrectStatusAndRecordException()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = Mvp24HoursActivitySources.Core.Source.StartActivity("Test");
        var exception = new InvalidOperationException("Test error");

        // Act
        activity.SetError(exception);

        // Assert
        activity.Should().NotBeNull();
        activity!.Status.Should().Be(ActivityStatusCode.Error);
        activity.GetTagItem(SemanticTags.OperationSuccess).Should().Be(false);
        activity.GetTagItem(SemanticTags.ErrorType).Should().Be(typeof(InvalidOperationException).FullName);
        activity.GetTagItem(SemanticTags.ErrorMessage).Should().Be("Test error");
        activity.Events.Should().Contain(e => e.Name == SemanticEvents.Exception);
    }

    [Fact]
    public void ActivityExtensions_WithCorrelationId_ShouldSetTagAndBaggage()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = Mvp24HoursActivitySources.Core.Source.StartActivity("Test");

        // Act
        activity.WithCorrelationId("test-correlation-123");

        // Assert
        activity.Should().NotBeNull();
        activity!.GetTagItem(SemanticTags.CorrelationId).Should().Be("test-correlation-123");
        activity.GetBaggageItem("correlation.id").Should().Be("test-correlation-123");
    }

    [Fact]
    public void ActivityExtensions_WithUser_ShouldSetUserTags()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = Mvp24HoursActivitySources.Core.Source.StartActivity("Test");

        // Act
        activity.WithUser("user-123", "John Doe");

        // Assert
        activity.Should().NotBeNull();
        activity!.GetTagItem(SemanticTags.EnduserId).Should().Be("user-123");
        activity.GetTagItem(SemanticTags.EnduserName).Should().Be("John Doe");
    }

    [Fact]
    public void ActivityExtensions_WithTenant_ShouldSetTenantTags()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = Mvp24HoursActivitySources.Core.Source.StartActivity("Test");

        // Act
        activity.WithTenant("tenant-456", "Acme Corp");

        // Assert
        activity.Should().NotBeNull();
        activity!.GetTagItem(SemanticTags.TenantId).Should().Be("tenant-456");
        activity.GetTagItem(SemanticTags.TenantName).Should().Be("Acme Corp");
    }

    [Fact]
    public void ActivityExtensions_RecordCacheHit_ShouldAddEvent()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = Mvp24HoursActivitySources.Core.Source.StartActivity("Test");

        // Act
        activity.RecordCacheHit("order:123");

        // Assert
        activity.Should().NotBeNull();
        activity!.GetTagItem(SemanticTags.CacheHit).Should().Be(true);
        activity.Events.Should().Contain(e => e.Name == SemanticEvents.CacheHit);
    }

    [Fact]
    public void ActivityExtensions_RecordRetryAttempt_ShouldAddEvent()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = Mvp24HoursActivitySources.Core.Source.StartActivity("Test");

        // Act
        activity.RecordRetryAttempt(2, TimeSpan.FromSeconds(5), "Timeout");

        // Assert
        activity.Should().NotBeNull();
        activity!.Events.Should().Contain(e => e.Name == SemanticEvents.RetryAttempt);
    }

    #endregion

    #region ScopedActivity Tests

    [Fact]
    public void ScopedActivity_OnDispose_ShouldSetSuccess_WhenNoException()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        Activity? capturedActivity = null;

        // Act
        using (var scope = Mvp24HoursActivitySources.Core.Source.StartScopedActivity("Test"))
        {
            capturedActivity = scope.Activity;
        }

        // Assert
        capturedActivity.Should().NotBeNull();
        capturedActivity!.Status.Should().Be(ActivityStatusCode.Ok);
    }

    [Fact]
    public void ScopedActivity_OnDispose_ShouldSetError_WhenExceptionSet()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        Activity? capturedActivity = null;
        var exception = new InvalidOperationException("Test error");

        // Act
        using (var scope = Mvp24HoursActivitySources.Core.Source.StartScopedActivity("Test"))
        {
            capturedActivity = scope.Activity;
            scope.SetException(exception);
        }

        // Assert
        capturedActivity.Should().NotBeNull();
        capturedActivity!.Status.Should().Be(ActivityStatusCode.Error);
    }

    #endregion

    #region TracePropagation Tests

    [Fact]
    public void TracePropagation_InjectAndExtract_ShouldWorkCorrectly()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var originalActivity = Mvp24HoursActivitySources.Core.Source.StartActivity("Original");

        // Act - Inject
        var headers = new Dictionary<string, string>();
        TracePropagation.InjectTraceContext(headers, originalActivity);

        // Act - Extract
        var headersWithNullable = headers.ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value);
        var extractedContext = TracePropagation.ExtractTraceContext(headersWithNullable);

        // Assert
        extractedContext.Should().NotBeNull();
        extractedContext!.TraceId.Should().Be(originalActivity!.TraceId.ToString());
        extractedContext.SpanId.Should().Be(originalActivity.SpanId.ToString());
    }

    [Fact]
    public void TracePropagation_ParseTraceparent_ShouldParseValidHeader()
    {
        // Arrange
        var traceparent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";

        // Act
        var context = ActivityHelper.ParseTraceContext(traceparent);

        // Assert
        context.TraceId.ToString().Should().Be("0af7651916cd43dd8448eb211c80319c");
        context.SpanId.ToString().Should().Be("b7ad6b7169203331");
    }

    #endregion

    #region Enricher Tests

    [Fact]
    public void CorrelationIdEnricher_ShouldEnrichActivity()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var enricher = new CorrelationIdEnricher
        {
            GetCorrelationId = () => "test-correlation"
        };

        using var activity = Mvp24HoursActivitySources.Core.Source.StartActivity("Test");

        // Act
        enricher.EnrichOnStart(activity!, null);

        // Assert
        activity!.GetTagItem(SemanticTags.CorrelationId).Should().Be("test-correlation");
    }

    [Fact]
    public void UserContextEnricher_ShouldEnrichActivity()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var enricher = new UserContextEnricher
        {
            GetUserId = () => "user-123",
            GetUserName = () => "John Doe",
            GetUserRoles = () => new[] { "Admin", "User" }
        };

        using var activity = Mvp24HoursActivitySources.Core.Source.StartActivity("Test");

        // Act
        enricher.EnrichOnStart(activity!, null);

        // Assert
        activity!.GetTagItem(SemanticTags.EnduserId).Should().Be("user-123");
        activity.GetTagItem(SemanticTags.EnduserName).Should().Be("John Doe");
        activity.GetTagItem(SemanticTags.EnduserRoles).Should().Be("Admin,User");
    }

    [Fact]
    public void TenantContextEnricher_ShouldEnrichActivity()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var enricher = new TenantContextEnricher
        {
            GetTenantId = () => "tenant-456",
            GetTenantName = () => "Acme Corp"
        };

        using var activity = Mvp24HoursActivitySources.Core.Source.StartActivity("Test");

        // Act
        enricher.EnrichOnStart(activity!, null);

        // Assert
        activity!.GetTagItem(SemanticTags.TenantId).Should().Be("tenant-456");
        activity.GetTagItem(SemanticTags.TenantName).Should().Be("Acme Corp");
    }

    [Fact]
    public void CompositeActivityEnricher_ShouldRunAllEnrichers()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var correlationEnricher = new CorrelationIdEnricher { GetCorrelationId = () => "test-corr" };
        var userEnricher = new UserContextEnricher { GetUserId = () => "user-1" };
        var tenantEnricher = new TenantContextEnricher { GetTenantId = () => "tenant-1" };

        var composite = new CompositeActivityEnricher(new IActivityEnricher[]
        {
            correlationEnricher,
            userEnricher,
            tenantEnricher
        });

        using var activity = Mvp24HoursActivitySources.Core.Source.StartActivity("Test");

        // Act
        composite.EnrichOnStart(activity!, null);

        // Assert
        activity!.GetTagItem(SemanticTags.CorrelationId).Should().Be("test-corr");
        activity.GetTagItem(SemanticTags.EnduserId).Should().Be("user-1");
        activity.GetTagItem(SemanticTags.TenantId).Should().Be("tenant-1");
    }

    #endregion

    #region DI Extensions Tests

    [Fact]
    public void AddMvp24HoursTracing_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMvp24HoursTracing();
        var provider = services.BuildServiceProvider();

        // Assert
        var accessor = provider.GetService<ITraceContextAccessor>();
        accessor.Should().NotBeNull();

        var options = provider.GetService<TracingOptions>();
        options.Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursTracing_WithOptions_ShouldConfigureCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMvp24HoursTracing(options =>
        {
            options.ServiceName = "TestService";
            options.EnableCorrelationIdPropagation = true;
            options.AddEnricher(new CorrelationIdEnricher());
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<TracingOptions>();
        options.Should().NotBeNull();
        options!.ServiceName.Should().Be("TestService");
        options.EnableCorrelationIdPropagation.Should().BeTrue();
    }

    [Fact]
    public void TraceContextAccessor_ShouldReturnCurrentActivityInfo()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var accessor = new TraceContextAccessor();
        using var activity = Mvp24HoursActivitySources.Core.Source.StartActivity("Test");

        // Act & Assert
        accessor.TraceId.Should().NotBeNull();
        accessor.SpanId.Should().NotBeNull();
        accessor.CurrentActivity.Should().Be(activity);
    }

    #endregion

    #region SemanticTags Tests

    [Fact]
    public void SemanticTags_ShouldHaveCorrectValues()
    {
        // Assert - General
        SemanticTags.CorrelationId.Should().Be("correlation.id");
        SemanticTags.CausationId.Should().Be("causation.id");
        SemanticTags.OperationName.Should().Be("operation.name");
        SemanticTags.OperationType.Should().Be("operation.type");

        // Assert - User
        SemanticTags.EnduserId.Should().Be("enduser.id");
        SemanticTags.EnduserName.Should().Be("enduser.name");

        // Assert - Tenant
        SemanticTags.TenantId.Should().Be("tenant.id");
        SemanticTags.TenantName.Should().Be("tenant.name");

        // Assert - Error
        SemanticTags.ErrorType.Should().Be("error.type");
        SemanticTags.ErrorMessage.Should().Be("error.message");

        // Assert - Database
        SemanticTags.DbSystem.Should().Be("db.system");
        SemanticTags.DbName.Should().Be("db.name");
        SemanticTags.DbOperation.Should().Be("db.operation");

        // Assert - Messaging
        SemanticTags.MessagingSystem.Should().Be("messaging.system");
        SemanticTags.MessagingDestinationName.Should().Be("messaging.destination.name");

        // Assert - Cache
        SemanticTags.CacheHit.Should().Be("cache.hit");
        SemanticTags.CacheKey.Should().Be("cache.key");
    }

    [Fact]
    public void SemanticEvents_ShouldHaveCorrectValues()
    {
        SemanticEvents.Exception.Should().Be("exception");
        SemanticEvents.RetryAttempt.Should().Be("retry.attempt");
        SemanticEvents.CacheHit.Should().Be("cache.hit");
        SemanticEvents.CacheMiss.Should().Be("cache.miss");
        SemanticEvents.ValidationFailure.Should().Be("validation.failure");
        SemanticEvents.SlowQueryDetected.Should().Be("slow_query.detected");
    }

    #endregion
}

