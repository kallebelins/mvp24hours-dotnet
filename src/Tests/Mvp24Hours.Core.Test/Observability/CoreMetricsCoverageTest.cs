using Mvp24Hours.Core.Observability;
using Mvp24Hours.Core.Observability.Metrics;
using Mvp24Hours.Infrastructure.Testing.Observability;
using Microsoft.Extensions.DependencyInjection;

namespace Mvp24Hours.Core.Test.Observability;

[Trait("Category", "Unit")]
public class CoreMetricsCoverageTest
{
    [Fact]
    public void CacheMetrics_RecordGetHitAndMiss_ShouldEmitCounters()
    {
        using var listener = new FakeMeterListener(Mvp24HoursMeters.Caching.Name);
        var metrics = new CacheMetrics();

        metrics.RecordGet("main", hit: true, durationMs: 1.5);
        metrics.RecordGet("main", hit: false, durationMs: 2.5);

        listener.GetSum(MetricNames.CacheGetsTotal).Should().Be(2);
        listener.GetSum(MetricNames.CacheHitsTotal).Should().Be(1);
        listener.GetSum(MetricNames.CacheMissesTotal).Should().Be(1);
    }

    [Fact]
    public void CacheMetrics_Scopes_ShouldRecordOperations()
    {
        using var listener = new FakeMeterListener(Mvp24HoursMeters.Caching.Name);
        var metrics = new CacheMetrics();

        using (var get = metrics.BeginGet("session"))
        {
            get.SetHit();
        }

        using (var set = metrics.BeginSet("session"))
        {
            set.SetItemSize(128);
        }

        using (metrics.BeginRemove("session"))
        {
        }

        metrics.RecordInvalidation("session", itemsInvalidated: 2);

        listener.GetSum(MetricNames.CacheGetsTotal).Should().Be(1);
        listener.GetSum(MetricNames.CacheSetsTotal).Should().Be(1);
        listener.GetSum(MetricNames.CacheRemovesTotal).Should().Be(1);
        listener.GetSum(MetricNames.CacheInvalidationsTotal).Should().Be(1);
    }

    [Fact]
    public void CqrsMetrics_CommandQueryNotificationScopes_ShouldEmitMetrics()
    {
        using var listener = new FakeMeterListener(Mvp24HoursMeters.Cqrs.Name);
        var metrics = new CqrsMetrics();

        using (var cmd = metrics.BeginCommand("CreateOrder"))
        {
            cmd.Complete();
        }

        using (var q = metrics.BeginQuery("GetOrder"))
        {
            q.Fail();
        }

        using (var n = metrics.BeginNotification("OrderCreated"))
        {
            n.Complete();
        }

        using (var b = metrics.BeginBehavior("Validation"))
        {
            b.Complete();
        }

        metrics.RecordDomainEvent("OrderCreated");
        metrics.RecordIntegrationEvent("OrderCreatedIntegration");
        metrics.RecordSagaStart("CheckoutSaga");
        metrics.RecordSagaCompleted("CheckoutSaga");
        metrics.RecordValidationFailure("CreateOrder");
        metrics.RecordCacheHit("GetOrder");
        metrics.RecordCacheMiss("GetOrder");
        metrics.RecordIdempotentDuplicate("CreateOrder");
        metrics.RecordRetry("CreateOrder", attemptNumber: 2);
        metrics.RecordCircuitBreakerTrip("CreateOrder");

        listener.GetSum(MetricNames.CqrsCommandsTotal).Should().Be(1);
        listener.GetSum(MetricNames.CqrsCommandsFailedTotal).Should().Be(0);
        listener.GetSum(MetricNames.CqrsQueriesTotal).Should().Be(1);
        listener.GetSum(MetricNames.CqrsQueriesFailedTotal).Should().Be(1);
        listener.GetSum(MetricNames.CqrsNotificationsTotal).Should().Be(1);
        listener.GetSum(MetricNames.CqrsBehaviorsTotal).Should().Be(1);
        listener.GetSum(MetricNames.CqrsDomainEventsTotal).Should().Be(1);
        listener.GetSum(MetricNames.CqrsIntegrationEventsTotal).Should().Be(1);
        listener.GetSum(MetricNames.CqrsSagasTotal).Should().Be(1);
        listener.GetSum(MetricNames.CqrsSagasCompletedTotal).Should().Be(1);
        listener.GetSum(MetricNames.CqrsValidationFailuresTotal).Should().Be(1);
        listener.GetSum(MetricNames.CqrsCacheHitsTotal).Should().Be(1);
        listener.GetSum(MetricNames.CqrsCacheMissesTotal).Should().Be(1);
        listener.GetSum(MetricNames.CqrsIdempotentDuplicatesTotal).Should().Be(1);
        listener.GetSum(MetricNames.CqrsRetriesTotal).Should().Be(1);
        listener.GetSum(MetricNames.CqrsCircuitBreakerTripsTotal).Should().Be(1);
    }

    [Fact]
    public void HttpMetrics_RequestScope_ShouldRecordRequest()
    {
        using var listener = new FakeMeterListener(Mvp24HoursMeters.WebAPI.Name);
        var metrics = new HttpMetrics();

        metrics.IncrementActiveRequests();
        using (var scope = metrics.BeginRequest("GET", "/api/orders"))
        {
            scope.SetStatusCode(200);
            scope.SetSizes(requestSize: 10, responseSize: 20);
        }

        metrics.DecrementActiveRequests();
        metrics.RecordRateLimitHit("/api/orders", "default");
        metrics.RecordIdempotentDuplicate("/api/orders");

        listener.GetSum(MetricNames.HttpRequestsTotal).Should().Be(1);
        listener.GetSum(MetricNames.HttpRateLimitHitsTotal).Should().Be(1);
        listener.GetSum(MetricNames.HttpIdempotentDuplicatesTotal).Should().Be(1);
    }

    [Fact]
    public void PipelineMetrics_ExecutionAndOperationScopes_ShouldRecord()
    {
        using var listener = new FakeMeterListener(Mvp24HoursMeters.Pipe.Name);
        var metrics = new PipelineMetrics();

        using (var pipeline = metrics.BeginExecution("checkout"))
        {
            pipeline.Complete();
        }

        using (var op = metrics.BeginOperation("checkout", "validate"))
        {
            op.Fail();
        }

        metrics.RecordExecution("checkout", durationMs: 12, success: true);
        metrics.RecordOperation("checkout", "persist", durationMs: 5, success: false);

        listener.GetSum(MetricNames.PipelineExecutionsTotal).Should().BeGreaterThan(0);
        listener.GetSum(MetricNames.PipelineOperationsTotal).Should().BeGreaterThan(0);
        listener.GetSum(MetricNames.PipelineOperationsFailedTotal).Should().BeGreaterThan(0);
    }

    [Fact]
    public void RepositoryMetrics_QueryCommandSaveScopes_ShouldRecord()
    {
        using var listener = new FakeMeterListener(Mvp24HoursMeters.Data.Name);
        var metrics = new RepositoryMetrics();

        using (var query = metrics.BeginQuery("List", "Order", "efcore"))
        {
            query.Complete();
        }

        using (var command = metrics.BeginCommand("Insert", "Order", "efcore"))
        {
            command.Complete(rowsAffected: 1);
        }

        using (var save = metrics.BeginSaveChanges("efcore"))
        {
            save.Complete(rowsAffected: 3);
        }

        metrics.RecordSlowQuery("List", "Order", durationMs: 1500, dbSystem: "efcore");
        metrics.RecordBulkOperation("BulkInsert", entityType: "Order", rowsAffected: 10, dbSystem: "efcore");
        metrics.RecordTransactionStart("efcore");
        metrics.RecordTransactionRollback("efcore");
        metrics.UpdateActiveConnections(delta: 1, dbSystem: "efcore");
        metrics.UpdateIdleConnections(delta: -1, dbSystem: "efcore");

        listener.GetSum(MetricNames.DataQueriesTotal).Should().Be(1);
        listener.GetSum(MetricNames.DataCommandsTotal).Should().Be(1);
        listener.GetSum(MetricNames.DataSaveChangesTotal).Should().Be(1);
        listener.GetSum(MetricNames.DataSlowQueriesTotal).Should().Be(1);
        listener.GetSum(MetricNames.DataBulkOperationsTotal).Should().Be(1);
        listener.GetSum(MetricNames.DataTransactionsTotal).Should().Be(1);
        listener.GetSum(MetricNames.DataTransactionRollbacksTotal).Should().Be(1);
    }

    [Fact]
    public void MessagingMetrics_PublishConsumeScopes_ShouldRecord()
    {
        using var listener = new FakeMeterListener(Mvp24HoursMeters.RabbitMQ.Name);
        var metrics = new MessagingMetrics();

        using (var publish = metrics.BeginPublish("OrderCreated", "orders"))
        {
            publish.Complete(payloadSize: 64);
        }

        using (var consume = metrics.BeginConsume("OrderCreated", "orders", consumerGroup: "workers"))
        {
            consume.Complete();
        }

        metrics.RecordAcknowledge("orders");
        metrics.RecordReject("orders", requeue: true);
        metrics.RecordDeadLetter("orders", "OrderCreated");
        metrics.RecordBatch("orders", batchSize: 5);
        metrics.UpdateQueueDepth("orders", delta: 3);
        metrics.UpdateActiveConsumers("orders", delta: 1);
        metrics.RecordConnectionAttempt(success: false);
        metrics.UpdateActiveConnections(delta: 1);
        metrics.RecordConnectionFailure("Timeout");

        listener.GetSum(MetricNames.MessagingPublishedTotal).Should().Be(1);
        listener.GetSum(MetricNames.MessagingConsumedTotal).Should().Be(1);
        listener.GetSum(MetricNames.MessagingAcknowledgedTotal).Should().Be(1);
        listener.GetSum(MetricNames.MessagingRequeuedTotal).Should().Be(1);
        listener.GetSum(MetricNames.MessagingDeadLetteredTotal).Should().Be(1);
        listener.GetSum(MetricNames.MessagingBatchesTotal).Should().Be(1);
        listener.GetSum(MetricNames.MessagingConnectionsTotal).Should().Be(1);
        listener.GetSum(MetricNames.MessagingConnectionFailuresTotal).Should().Be(2);
    }

    [Fact]
    public void InfrastructureMetrics_CrossCuttingOperations_ShouldRecord()
    {
        using var listener = new FakeMeterListener(Mvp24HoursMeters.Infrastructure.Name);
        var metrics = new InfrastructureMetrics();

        using (var http = metrics.BeginHttpClientRequest("GET", "api.example.com"))
        {
            http.SetStatusCode(200);
        }

        metrics.RecordEmailSend(success: true, provider: "smtp");
        metrics.RecordSmsSend(success: false, provider: "twilio");
        metrics.RecordFileStorageOperation("upload", fileSizeBytes: 1024, provider: "s3");

        using (var lockScope = metrics.BeginLock("resource-1"))
        {
        }

        metrics.RecordLockAttempt("resource-1", acquired: true, waitDurationMs: 2);
        metrics.RecordLockRelease("resource-1", holdDurationMs: 10);

        using (var job = metrics.BeginBackgroundJob("cleanup"))
        {
            job.Complete();
        }

        metrics.UpdatePendingJobs(delta: 2, jobQueue: "default");

        listener.GetSum(MetricNames.HttpClientRequestsTotal).Should().Be(1);
        listener.GetSum(MetricNames.EmailsSentTotal).Should().Be(1);
        listener.GetSum(MetricNames.SmsFailedTotal).Should().Be(1);
        listener.GetSum(MetricNames.FileStorageOperationsTotal).Should().Be(1);
        listener.GetSum(MetricNames.DistributedLockAcquisitionsTotal).Should().BeGreaterThan(0);
        listener.GetSum(MetricNames.BackgroundJobsTotal).Should().Be(1);
    }

    [Fact]
    public void CronJobMetrics_JobScope_ShouldRecord()
    {
        using var listener = new FakeMeterListener(Mvp24HoursMeters.CronJob.Name);
        var metrics = new CronJobMetrics();

        metrics.IncrementActive("sync");
        using (var job = metrics.BeginExecution("sync"))
        {
            job.Complete();
        }

        metrics.DecrementActive("sync");
        metrics.UpdateScheduledCount(delta: 1);

        listener.GetSum(MetricNames.CronJobExecutionsTotal).Should().Be(1);
    }

    [Fact]
    public void MetricsServiceExtensions_AddMvp24HoursMetrics_ShouldRegisterEnabledTypes()
    {
        var services = new ServiceCollection();
        services.AddMvp24HoursMetrics(options =>
        {
            options.EnablePipelineMetrics = true;
            options.EnableCqrsMetrics = true;
            options.EnableRepositoryMetrics = true;
            options.EnableMessagingMetrics = true;
            options.EnableHttpMetrics = true;
            options.EnableCacheMetrics = true;
            options.EnableInfrastructureMetrics = true;
            options.EnableCronJobMetrics = true;
        });

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<PipelineMetrics>().Should().NotBeNull();
        provider.GetRequiredService<CqrsMetrics>().Should().NotBeNull();
        provider.GetRequiredService<RepositoryMetrics>().Should().NotBeNull();
        provider.GetRequiredService<MessagingMetrics>().Should().NotBeNull();
        provider.GetRequiredService<HttpMetrics>().Should().NotBeNull();
        provider.GetRequiredService<CacheMetrics>().Should().NotBeNull();
        provider.GetRequiredService<InfrastructureMetrics>().Should().NotBeNull();
        provider.GetRequiredService<CronJobMetrics>().Should().NotBeNull();
    }

    [Fact]
    public void MetricsServiceExtensions_IndividualRegistrations_ShouldRegisterSingleMetricType()
    {
        var services = new ServiceCollection();

        services.AddPipelineMetrics();
        services.AddRepositoryMetrics();
        services.AddCqrsMetrics();
        services.AddMessagingMetrics();
        services.AddCacheMetrics();
        services.AddHttpMetrics();
        services.AddCronJobMetrics();
        services.AddInfrastructureMetrics();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<PipelineMetrics>().Should().NotBeNull();
        provider.GetRequiredService<RepositoryMetrics>().Should().NotBeNull();
        provider.GetRequiredService<CqrsMetrics>().Should().NotBeNull();
        provider.GetRequiredService<MessagingMetrics>().Should().NotBeNull();
        provider.GetRequiredService<CacheMetrics>().Should().NotBeNull();
        provider.GetRequiredService<HttpMetrics>().Should().NotBeNull();
        provider.GetRequiredService<CronJobMetrics>().Should().NotBeNull();
        provider.GetRequiredService<InfrastructureMetrics>().Should().NotBeNull();
    }

    [Fact]
    public void MetricsServiceExtensions_AddMvp24HoursMetrics_ShouldRespectDisabledOptions()
    {
        var services = new ServiceCollection();
        services.AddMvp24HoursMetrics(options =>
        {
            options.EnablePipelineMetrics = false;
            options.EnableCqrsMetrics = false;
            options.EnableRepositoryMetrics = false;
            options.EnableMessagingMetrics = false;
            options.EnableHttpMetrics = false;
            options.EnableCacheMetrics = false;
            options.EnableInfrastructureMetrics = false;
            options.EnableCronJobMetrics = false;
        });

        using var provider = services.BuildServiceProvider();
        provider.GetService<PipelineMetrics>().Should().BeNull();
        provider.GetService<CqrsMetrics>().Should().BeNull();
        provider.GetService<RepositoryMetrics>().Should().BeNull();
        provider.GetService<MessagingMetrics>().Should().BeNull();
        provider.GetService<HttpMetrics>().Should().BeNull();
        provider.GetService<CacheMetrics>().Should().BeNull();
        provider.GetService<InfrastructureMetrics>().Should().BeNull();
        provider.GetService<CronJobMetrics>().Should().BeNull();
    }

    [Fact]
    public void OpenTelemetryMeterBuilderExtensions_GetMvp24HoursMeterNames_ShouldFilterModules()
    {
        string[] all = OpenTelemetryMeterBuilderExtensions.GetMvp24HoursMeterNames();
        all.Should().HaveCount(Mvp24HoursMeters.AllMeterNames.Length);

        string[] dataOnly = OpenTelemetryMeterBuilderExtensions.GetMvp24HoursMeterNames(
            includeCore: false,
            includePipe: false,
            includeCqrs: false,
            includeData: true,
            includeRabbitMQ: false,
            includeWebAPI: false,
            includeCaching: false,
            includeCronJob: false,
            includeInfrastructure: false);

        dataOnly.Should().ContainSingle(name => name == Mvp24HoursMeters.Data.Name);
    }

    [Fact]
    public void CqrsMetrics_FailedCommandAndRetryScopes_ShouldEmitFailureCounters()
    {
        using var listener = new FakeMeterListener(Mvp24HoursMeters.Cqrs.Name);
        var metrics = new CqrsMetrics();

        using (var cmd = metrics.BeginCommand("DeleteOrder"))
        {
            cmd.Fail();
        }

        metrics.RecordSagaFailed("CheckoutSaga");
        metrics.RecordRetry("DeleteOrder", attemptNumber: 1);

        listener.GetSum(MetricNames.CqrsCommandsTotal).Should().Be(1);
        listener.GetSum(MetricNames.CqrsCommandsFailedTotal).Should().Be(1);
        listener.GetSum(MetricNames.CqrsSagasFailedTotal).Should().Be(1);
    }

    [Fact]
    public void RepositoryMetrics_FailedScopes_ShouldEmitFailureCounters()
    {
        using var listener = new FakeMeterListener(Mvp24HoursMeters.Data.Name);
        var metrics = new RepositoryMetrics();

        using (var query = metrics.BeginQuery("Select", "Order", "efcore"))
        {
            query.Fail();
        }

        using (var command = metrics.BeginCommand("Delete", "Order", "efcore"))
        {
            command.Fail();
        }

        using (var save = metrics.BeginSaveChanges("efcore"))
        {
            save.Fail();
        }

        listener.GetSum(MetricNames.DataQueriesFailedTotal).Should().Be(1);
        listener.GetSum(MetricNames.DataCommandsFailedTotal).Should().Be(1);
        listener.GetSum(MetricNames.DataSaveChangesTotal).Should().Be(1);
    }

    [Fact]
    public void MessagingMetrics_FailedPublishAndConsume_ShouldEmitFailureCounters()
    {
        using var listener = new FakeMeterListener(Mvp24HoursMeters.RabbitMQ.Name);
        var metrics = new MessagingMetrics();

        using (var publish = metrics.BeginPublish("OrderCreated", "orders"))
        {
            publish.Fail();
        }

        using (var consume = metrics.BeginConsume("OrderCreated", "orders", consumerGroup: "workers"))
        {
            consume.Fail();
        }

        metrics.RecordReject("orders", requeue: false);

        listener.GetSum(MetricNames.MessagingPublishFailedTotal).Should().Be(1);
        listener.GetSum(MetricNames.MessagingConsumeFailedTotal).Should().Be(1);
        listener.GetSum(MetricNames.MessagingRejectedTotal).Should().Be(1);
    }

    [Fact]
    public void InfrastructureMetrics_FailedHttpAndBackgroundJob_ShouldEmitFailureCounters()
    {
        using var listener = new FakeMeterListener(Mvp24HoursMeters.Infrastructure.Name);
        var metrics = new InfrastructureMetrics();

        using (var http = metrics.BeginHttpClientRequest("POST", "api.example.com"))
        {
            http.SetStatusCode(500);
        }

        metrics.RecordEmailSend(success: false, provider: "smtp");
        metrics.RecordSmsSend(success: true, provider: "twilio");

        using (var job = metrics.BeginBackgroundJob("cleanup"))
        {
            job.Fail();
        }

        listener.GetSum(MetricNames.HttpClientRequestsFailedTotal).Should().Be(1);
        listener.GetSum(MetricNames.EmailsFailedTotal).Should().Be(1);
        listener.GetSum(MetricNames.SmsSentTotal).Should().Be(1);
        listener.GetSum(MetricNames.BackgroundJobsFailedTotal).Should().Be(1);
    }

    [Fact]
    public void CronJobMetrics_FailedExecution_ShouldEmitFailureCounter()
    {
        using var listener = new FakeMeterListener(Mvp24HoursMeters.CronJob.Name);
        var metrics = new CronJobMetrics();

        using (var job = metrics.BeginExecution("sync"))
        {
            job.Fail();
        }

        listener.GetSum(MetricNames.CronJobExecutionsFailedTotal).Should().Be(1);
    }
}
