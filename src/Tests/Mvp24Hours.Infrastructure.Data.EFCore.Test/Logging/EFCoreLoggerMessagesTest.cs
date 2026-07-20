using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Infrastructure.Data.EFCore.Logging;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Logging;

[Trait("Category", "Unit")]
public class EFCoreLoggerMessagesTest
{
    [Fact]
    public void LoggerMessageMethods_ShouldNotThrowWithNullLogger()
    {
        ILogger logger = NullLogger.Instance;

        var act = () =>
        {
            EFCoreLoggerMessages.QueryExecuted(logger, "GetAll", "Customer", 12);
            EFCoreLoggerMessages.SlowQueryDetected(logger, "GetAll", "Customer", 1500, 1000);
            EFCoreLoggerMessages.QueryWithSpecification(logger, "ActiveSpec", 5);
            EFCoreLoggerMessages.CompiledQueryExecuted(logger, "ById", 3);
            EFCoreLoggerMessages.EntityOperation(logger, "Insert", "Customer", "1");
            EFCoreLoggerMessages.CommandExecuted(logger, "SELECT 1", 2, 1);
            EFCoreLoggerMessages.TransactionStarted(logger, "tx-1");
            EFCoreLoggerMessages.TransactionCommitted(logger, "tx-1", 10);
            EFCoreLoggerMessages.TransactionRolledBack(logger, null, "tx-1");
            EFCoreLoggerMessages.ConnectionOpened(logger, "AppDb");
            EFCoreLoggerMessages.ConnectionClosed(logger, "AppDb", 5);
            EFCoreLoggerMessages.ConnectionFailed(logger, new InvalidOperationException("fail"), "AppDb");
            EFCoreLoggerMessages.ConnectionPoolStatus(logger, 2, 8);
            EFCoreLoggerMessages.SaveChangesStarted(logger, 3, 1, 1, 1);
            EFCoreLoggerMessages.SaveChangesCompleted(logger, 15, 2);
            EFCoreLoggerMessages.SaveChangesFailed(logger, new InvalidOperationException("save"));
            EFCoreLoggerMessages.AuditEntry(logger, "Create", "Customer", "user-1", "10");
            EFCoreLoggerMessages.AuditFieldsSet(logger, "Customer", "user-1");
            EFCoreLoggerMessages.SoftDeleteApplied(logger, "Customer", "10", "user-1");
            EFCoreLoggerMessages.SoftDeleteRestored(logger, "Customer", "10");
            EFCoreLoggerMessages.ConcurrencyConflict(logger, "Customer", "10");
            EFCoreLoggerMessages.ConcurrencyTokenUpdated(logger, "Customer", "10", "v2");
            EFCoreLoggerMessages.MigrationStarted(logger, "AppDb");
            EFCoreLoggerMessages.MigrationCompleted(logger, 2);
            EFCoreLoggerMessages.MigrationApplied(logger, "20260101000000_Init");
            EFCoreLoggerMessages.PendingMigrations(logger, 1);
            EFCoreLoggerMessages.TenantContextApplied(logger, "tenant-1");
            EFCoreLoggerMessages.TenantFilterApplied(logger, "Customer", "tenant-1");
            EFCoreLoggerMessages.BulkOperationStarted(logger, "Insert", "Customer", 100);
            EFCoreLoggerMessages.BulkOperationCompleted(logger, "Insert", "Customer", 100, 50);
            EFCoreLoggerMessages.BulkOperationProgress(logger, 50, 100, 50);
            EFCoreLoggerMessages.HealthCheckResult(logger, "Healthy", 8);
            EFCoreLoggerMessages.HealthCheckDegraded(logger, "slow");
            EFCoreLoggerMessages.RetryAttempt(logger, 1, 3, "SaveChanges", 100);
            EFCoreLoggerMessages.CircuitBreakerOpened(logger, 5);
            EFCoreLoggerMessages.CircuitBreakerClosed(logger);
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void EventIdConstants_ShouldBeUniqueAndIn5000Range()
    {
        IEnumerable<FieldInfo> eventIdFields = typeof(EFCoreLoggerMessages)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(int) && f.Name.EndsWith("EventId"));

        var values = eventIdFields.Select(f => (Name: f.Name, Value: (int)f.GetRawConstantValue()!)).ToList();

        values.Should().NotBeEmpty();
        values.Select(v => v.Value).Should().OnlyHaveUniqueItems();
        values.Should().OnlyContain(v => v.Value >= 5000 && v.Value < 6000);
        values.Should().Contain(v => v.Name == nameof(EFCoreLoggerMessages.QueryExecutedEventId) && v.Value == 5001);
        values.Should().Contain(v => v.Name == nameof(EFCoreLoggerMessages.CircuitBreakerClosedEventId) && v.Value == 5036);
    }
}
