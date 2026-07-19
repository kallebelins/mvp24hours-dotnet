using Mvp24Hours.Application.Contract.Observability;
using Mvp24Hours.Application.Logic.Observability;

namespace Mvp24Hours.Application.Test.Logic.Observability;

[Trait("Category", "Unit")]
public class InMemoryApplicationAuditStoreTest
{
    [Fact]
    public async Task SaveAsync_AndGetByCorrelationId_ShouldReturnMatchingEntries()
    {
        var store = new InMemoryApplicationAuditStore();
        var entry = new ApplicationAuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            CorrelationId = "corr-audit",
            UserId = "user-1",
            Timestamp = DateTimeOffset.UtcNow
        };

        await store.SaveAsync(entry);

        IList<ApplicationAuditEntry> results = await store.GetByCorrelationIdAsync("corr-audit");

        results.Should().ContainSingle(e => e.Id == entry.Id);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldFilterByDateRange()
    {
        var store = new InMemoryApplicationAuditStore();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        await store.SaveAsync(new ApplicationAuditEntry
        {
            Id = "1",
            UserId = "user-1",
            Timestamp = now.AddMinutes(-5)
        });
        await store.SaveAsync(new ApplicationAuditEntry
        {
            Id = "2",
            UserId = "user-1",
            Timestamp = now.AddDays(-2)
        });

        IList<ApplicationAuditEntry> results = await store.GetByUserIdAsync(
            "user-1",
            now.AddHours(-1),
            now.AddHours(1));

        results.Should().ContainSingle(e => e.Id == "1");
    }

    [Fact]
    public async Task GetByEntityAsync_ShouldReturnEntriesWithEntityId()
    {
        var store = new InMemoryApplicationAuditStore();
        await store.SaveAsync(new ApplicationAuditEntry
        {
            Id = "e1",
            EntityType = "Product",
            EntityIds = "42",
            Timestamp = DateTimeOffset.UtcNow
        });

        IList<ApplicationAuditEntry> results = await store.GetByEntityAsync("Product", "42");

        results.Should().ContainSingle(e => e.Id == "e1");
    }

    [Fact]
    public void Clear_ShouldRemoveAllEntries()
    {
        var store = new InMemoryApplicationAuditStore();
        store.SaveAsync(new ApplicationAuditEntry { Id = "x", Timestamp = DateTimeOffset.UtcNow })
            .GetAwaiter().GetResult();

        store.Clear();

        store.Count.Should().Be(0);
        store.GetAll().Should().BeEmpty();
    }
}
