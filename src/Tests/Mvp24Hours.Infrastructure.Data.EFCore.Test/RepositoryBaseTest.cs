using System.Reflection;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Helpers;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test;

[Trait("Category", "Unit")]
public class RepositoryBaseTest
{
    [Fact]
    public void Ctor_WhenDbContextIsNull_ShouldThrow()
    {
        Action act = () => _ = new TestableRepositoryBase(null!, EfCoreTestHelpers.CreateRepositoryOptions());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("_dbContext");
    }

    [Fact]
    public void Ctor_WhenOptionsIsNull_ShouldUseDefaults()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();

        var repository = new TestableRepositoryBase(context, null!);

        repository.ExposedOptions.MaxQtyByQueryPage.Should().Be(ConstantsHelper.Data.MaxQtyByQueryPage);
        repository.ExposedOptions.TransactionIsolationLevel.Should().BeNull();
    }

    [Fact]
    public void GetQuery_WithNullCriteria_ShouldApplyMaxQtyByQueryPage()
    {
        using TestDbContext context = SeedEntities(5);
        var repository = new TestableRepositoryBase(
            context,
            EfCoreTestHelpers.CreateRepositoryOptions(o => o.MaxQtyByQueryPage = 2));

        var results = repository.GetQuery(null).ToList();

        results.Should().HaveCount(2);
    }

    [Fact]
    public void GetQuery_WithCriteria_ShouldApplyOrderByOffsetAndLimit()
    {
        using TestDbContext context = SeedEntities(5);
        var repository = new TestableRepositoryBase(
            context,
            EfCoreTestHelpers.CreateRepositoryOptions(o => o.MaxQtyByQueryPage = 100));
        var criteria = new PagingCriteria(
            limit: 2,
            offset: 1,
            orderBy: ["Name"]);

        var results = repository.GetQuery(criteria).ToList();

        results.Should().HaveCount(2);
        results.Select(e => e.Name).Should().BeInAscendingOrder();
        results[0].Name.Should().Be("Entity-3");
        results[1].Name.Should().Be("Entity-4");
    }

    [Fact]
    public void GetQuery_WhenOnlyNavigation_ShouldSkipPaging()
    {
        using TestDbContext context = SeedEntities(5);
        var repository = new TestableRepositoryBase(
            context,
            EfCoreTestHelpers.CreateRepositoryOptions(o => o.MaxQtyByQueryPage = 2));

        var results = repository.GetQuery(null, onlyNavigation: true).ToList();

        results.Should().HaveCount(5);
    }

    [Fact]
    public void GetKeyInfo_ShouldReturnIdProperty()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        var repository = new TestableRepositoryBase(context, EfCoreTestHelpers.CreateRepositoryOptions());

        PropertyInfo key = repository.GetKeyInfo();

        key.Name.Should().Be(nameof(TestEntity.Id));
    }

    [Fact]
    public void GetDynamicFilter_ById_ShouldFilterEntity()
    {
        using TestDbContext context = SeedEntities(3);
        var repository = new TestableRepositoryBase(context, EfCoreTestHelpers.CreateRepositoryOptions());
        TestEntity target = context.Entities.OrderBy(e => e.Id).Skip(1).First();
        PropertyInfo key = repository.GetKeyInfo();

        TestEntity? found = repository.GetDynamicFilterPublic(context.Entities.AsQueryable(), key, target.Id)
            .SingleOrDefault();

        found.Should().NotBeNull();
        found!.Id.Should().Be(target.Id);
        found.Name.Should().Be(target.Name);
    }

    [Fact]
    public void CreateTransactionScope_WhenIsolationNullAndNotAggregate_ShouldReturnNull()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        var repository = new TestableRepositoryBase(
            context,
            EfCoreTestHelpers.CreateRepositoryOptions(o => o.TransactionIsolationLevel = null));

        using TransactionScope? scope = repository.CreateTransactionScope(isAggregate: false);

        scope.Should().BeNull();
    }

    [Fact]
    public void CreateTransactionScope_WhenIsAggregate_ShouldReturnNonNull()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        var repository = new TestableRepositoryBase(
            context,
            EfCoreTestHelpers.CreateRepositoryOptions(o => o.TransactionIsolationLevel = null));

        using TransactionScope? scope = repository.CreateTransactionScope(isAggregate: true);

        scope.Should().NotBeNull();
    }

    [Fact]
    public void CreateTransactionScope_WhenIsolationSet_ShouldReturnNonNull()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        var repository = new TestableRepositoryBase(
            context,
            EfCoreTestHelpers.CreateRepositoryOptions(o => o.TransactionIsolationLevel = IsolationLevel.ReadCommitted));

        using TransactionScope? scope = repository.CreateTransactionScope(isAggregate: false);

        scope.Should().NotBeNull();
    }

    private static TestDbContext SeedEntities(int count)
    {
        TestDbContext context = EfCoreTestHelpers.CreateContext();
        context.Entities.AddRange(EfCoreTestHelpers.CreateEntities(count));
        context.SaveChanges();
        return context;
    }

    private sealed class TestableRepositoryBase(DbContext dbContext, IOptions<EFCoreRepositoryOptions> options) : RepositoryBase<TestEntity>(dbContext, options)
    {
        protected override object? EntityLogBy => null;

        public EFCoreRepositoryOptions ExposedOptions => Options;

        public new IQueryable<TestEntity> GetQuery(IPagingCriteria? criteria, bool onlyNavigation = false)
        {
            return base.GetQuery(criteria, onlyNavigation);
        }

        public new PropertyInfo GetKeyInfo()
        {
            return base.GetKeyInfo();
        }

        public IQueryable<TestEntity> GetDynamicFilterPublic(IQueryable<TestEntity> query, PropertyInfo key, int value)
        {
            return GetDynamicFilter(query, key, value);
        }

        public new TransactionScope? CreateTransactionScope(bool isAggregate = false)
        {
            return base.CreateTransactionScope(isAggregate);
        }
    }
}
