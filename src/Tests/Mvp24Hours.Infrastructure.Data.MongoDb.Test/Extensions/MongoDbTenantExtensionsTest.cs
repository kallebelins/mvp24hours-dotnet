using MongoDB.Driver;
using Moq;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Extensions;

[Trait("Category", "Unit")]
public class MongoDbTenantExtensionsTest
{
    [Fact]
    public void ApplyTenantFilter_WithNullProvider_ShouldThrowArgumentNullException()
    {
        IQueryable<TenantInvoice> query = CreateInvoices().AsQueryable();

        Action act = () => query.ApplyTenantFilter((ITenantProvider)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("tenantProvider");
    }

    [Fact]
    public void ApplyTenantFilter_ForNonTenantEntity_ShouldReturnUnchangedQuery()
    {
        IQueryable<TestEntity> query = new List<TestEntity>
        {
            new() { Name = "A" },
            new() { Name = "B" }
        }.AsQueryable();

        IQueryable<TestEntity> result = query.ApplyTenantFilter(new FakeTenantProvider("tenant-a"));

        result.Should().HaveCount(2);
    }

    [Fact]
    public void ApplyTenantFilter_WithEmptyTenant_ShouldBypassFilter()
    {
        IQueryable<TenantInvoice> query = CreateInvoices().AsQueryable();

        IQueryable<TenantInvoice> result = query.ApplyTenantFilter(new FakeTenantProvider(null));

        result.Should().HaveCount(3);
    }

    [Fact]
    public void ApplyTenantFilter_WithTenantProvider_ShouldFilterByTenant()
    {
        IQueryable<TenantInvoice> query = CreateInvoices().AsQueryable();

        IQueryable<TenantInvoice> result = query.ApplyTenantFilter(new FakeTenantProvider("tenant-a"));

        result.Should().HaveCount(2);
        result.Should().OnlyContain(i => i.TenantId == "tenant-a");
    }

    [Fact]
    public void ApplyTenantFilter_WithStringTenantId_ShouldFilterByTenant()
    {
        IQueryable<TenantInvoice> query = CreateInvoices().AsQueryable();

        IQueryable<TenantInvoice> result = query.ApplyTenantFilter("tenant-b");

        result.Should().ContainSingle(i => i.TenantId == "tenant-b");
    }

    [Fact]
    public void ApplyTenantFilter_WithEmptyStringTenantId_ShouldBypassFilter()
    {
        IQueryable<TenantInvoice> query = CreateInvoices().AsQueryable();

        IQueryable<TenantInvoice> result = query.ApplyTenantFilter(string.Empty);

        result.Should().HaveCount(3);
    }

    [Fact]
    public void ApplyTenantFilter_WithGenericTenantId_ShouldFilterByGuidTenant()
    {
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        IQueryable<TenantOrder> query = new List<TenantOrder>
        {
            new() { TenantId = tenantId, Code = "A" },
            new() { TenantId = Guid.NewGuid(), Code = "B" }
        }.AsQueryable();

        IQueryable<TenantOrder> result = query.ApplyTenantFilter(tenantId);

        result.Should().ContainSingle(o => o.Code == "A");
    }

    [Fact]
    public void ApplyTenantFilter_WithDefaultGenericTenantId_ShouldBypassFilter()
    {
        IQueryable<TenantOrder> query = new List<TenantOrder>
        {
            new() { TenantId = Guid.NewGuid(), Code = "A" },
            new() { TenantId = Guid.NewGuid(), Code = "B" }
        }.AsQueryable();

        IQueryable<TenantOrder> result = query.ApplyTenantFilter(Guid.Empty);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void WithTenantFilter_WithNullProvider_ShouldThrowArgumentNullException()
    {
        FilterDefinition<TenantInvoice> filter = Builders<TenantInvoice>.Filter.Empty;

        Action act = () => filter.WithTenantFilter((ITenantProvider)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("tenantProvider");
    }

    [Fact]
    public void WithTenantFilter_WithEmptyTenant_ShouldReturnOriginalFilter()
    {
        FilterDefinition<TenantInvoice> filter = Builders<TenantInvoice>.Filter.Eq(i => i.Amount, 10m);

        FilterDefinition<TenantInvoice> result = filter.WithTenantFilter(new FakeTenantProvider(null));

        result.Should().BeSameAs(filter);
    }

    [Fact]
    public void WithTenantFilter_WithTenantProvider_ShouldCombineFilters()
    {
        FilterDefinition<TenantInvoice> filter = Builders<TenantInvoice>.Filter.Gt(i => i.Amount, 0m);

        FilterDefinition<TenantInvoice> result = filter.WithTenantFilter(new FakeTenantProvider("tenant-a"));

        result.Should().NotBeSameAs(filter);
        result.Should().NotBe(Builders<TenantInvoice>.Filter.Empty);
    }

    [Fact]
    public void WithTenantFilter_WithStringTenantId_ShouldCombineFilters()
    {
        FilterDefinition<TenantInvoice> filter = Builders<TenantInvoice>.Filter.Empty;

        FilterDefinition<TenantInvoice> result = filter.WithTenantFilter("tenant-c");

        result.Should().NotBeSameAs(filter);
    }

    [Fact]
    public void TenantFilter_WithNoTenant_ShouldReturnEmptyFilter()
    {
        FilterDefinition<TenantInvoice> filter = MongoDbTenantExtensions.TenantFilter<TenantInvoice>(new FakeTenantProvider(null));

        filter.Should().Be(Builders<TenantInvoice>.Filter.Empty);
    }

    [Fact]
    public void TenantFilter_WithTenant_ShouldReturnTenantFilter()
    {
        FilterDefinition<TenantInvoice> filter = MongoDbTenantExtensions.TenantFilter<TenantInvoice>(new FakeTenantProvider("tenant-a"));

        filter.Should().NotBe(Builders<TenantInvoice>.Filter.Empty);
    }

    [Fact]
    public void MatchTenant_WithTenantProvider_ShouldAddMatchStage()
    {
        var aggregateMock = new Mock<IAggregateFluent<TenantInvoice>>();
        aggregateMock
            .Setup(a => a.Match(It.IsAny<FilterDefinition<TenantInvoice>>()))
            .Returns(aggregateMock.Object);

        IAggregateFluent<TenantInvoice> result = aggregateMock.Object.MatchTenant(new FakeTenantProvider("tenant-a"));

        result.Should().BeSameAs(aggregateMock.Object);
        aggregateMock.Verify(a => a.Match(It.IsAny<FilterDefinition<TenantInvoice>>()), Times.Once);
    }

    [Fact]
    public void MatchTenant_WithEmptyTenant_ShouldNotAddMatchStage()
    {
        var aggregateMock = new Mock<IAggregateFluent<TenantInvoice>>();

        IAggregateFluent<TenantInvoice> result = aggregateMock.Object.MatchTenant(new FakeTenantProvider(null));

        result.Should().BeSameAs(aggregateMock.Object);
        aggregateMock.Verify(a => a.Match(It.IsAny<FilterDefinition<TenantInvoice>>()), Times.Never);
    }

    [Fact]
    public void MatchTenant_WithStringTenantId_ShouldAddMatchStage()
    {
        var aggregateMock = new Mock<IAggregateFluent<TenantInvoice>>();
        aggregateMock
            .Setup(a => a.Match(It.IsAny<FilterDefinition<TenantInvoice>>()))
            .Returns(aggregateMock.Object);

        IAggregateFluent<TenantInvoice> result = aggregateMock.Object.MatchTenant("tenant-b");

        aggregateMock.Verify(a => a.Match(It.IsAny<FilterDefinition<TenantInvoice>>()), Times.Once);
    }

    [Fact]
    public void ApplyGlobalFilters_ShouldApplyTenantAndSoftDeleteFilters()
    {
        IQueryable<AuditableTenantProduct> query = new List<AuditableTenantProduct>
        {
            new() { TenantId = "tenant-a", Name = "Active", IsDeleted = false },
            new() { TenantId = "tenant-a", Name = "Deleted", IsDeleted = true },
            new() { TenantId = "tenant-b", Name = "OtherTenant", IsDeleted = false }
        }.AsQueryable();

        IQueryable<AuditableTenantProduct> result = query.ApplyGlobalFilters(new FakeTenantProvider("tenant-a"));

        result.Should().ContainSingle(p => p.Name == "Active");
    }

    [Fact]
    public void CreateGlobalFilter_ShouldCombineTenantAndSoftDeleteFilters()
    {
        FilterDefinition<AuditableTenantProduct> filter =
            MongoDbTenantExtensions.CreateGlobalFilter<AuditableTenantProduct>(new FakeTenantProvider("tenant-a"));

        filter.Should().NotBe(Builders<AuditableTenantProduct>.Filter.Empty);
    }

    [Fact]
    public void CreateGlobalFilter_WithIncludeSoftDeleted_ShouldOnlyApplyTenantFilter()
    {
        FilterDefinition<AuditableTenantProduct> withSoftDelete =
            MongoDbTenantExtensions.CreateGlobalFilter<AuditableTenantProduct>(new FakeTenantProvider("tenant-a"));

        FilterDefinition<AuditableTenantProduct> includeDeleted =
            MongoDbTenantExtensions.CreateGlobalFilter<AuditableTenantProduct>(
                new FakeTenantProvider("tenant-a"),
                includeSoftDeleted: true);

        includeDeleted.Should().NotBe(withSoftDelete);
    }

    private static List<TenantInvoice> CreateInvoices()
    {
        return [
        new() { TenantId = "tenant-a", Amount = 10m },
        new() { TenantId = "tenant-a", Amount = 20m },
        new() { TenantId = "tenant-b", Amount = 30m }
    ];
    }
}
