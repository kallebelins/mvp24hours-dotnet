using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Domain.Specifications;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Data.MongoDb;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test;

[Trait("Category", "Integration")]
[Collection(MongoDbIntegrationCollection.Name)]
public class ReadOnlyRepositoryIntegrationTest(MongoDbIntegrationFixture fixture)
{
    private ReadOnlyRepository<TestEntity> CreateRepository()
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture);
        return new ReadOnlyRepository<TestEntity>(context, MongoDbIntegrationTestHelper.CreateRepositoryOptions());
    }

    private async Task SeedAsync(params TestEntity[] entities)
    {
        IMongoCollection<TestEntity> collection = fixture.GetCollection<TestEntity>();
        await collection.DeleteManyAsync(FilterDefinition<TestEntity>.Empty);
        if (entities.Length > 0)
        {
            await collection.InsertManyAsync(entities);
        }
    }

    [DockerFact]
    public void ListAny_And_ListCount_ShouldReflectStoredEntities()
    {
        SeedAsync(new TestEntity { Name = "A" }, new TestEntity { Name = "B" }).GetAwaiter().GetResult();
        ReadOnlyRepository<TestEntity> repository = CreateRepository();

        repository.ListAny().Should().BeTrue();
        repository.ListCount().Should().Be(2);
    }

    [DockerFact]
    public void GetBy_ShouldFilterEntities()
    {
        SeedAsync(new TestEntity { Name = "Match" }, new TestEntity { Name = "Other" }).GetAwaiter().GetResult();
        ReadOnlyRepository<TestEntity> repository = CreateRepository();

        repository.GetByAny(e => e.Name == "Match").Should().BeTrue();
        repository.GetByCount(e => e.Name == "Match").Should().Be(1);
        repository.GetBy(e => e.Name == "Match").Should().ContainSingle().Which.Name.Should().Be("Match");
    }

    [DockerFact]
    public void GetById_ShouldReturnEntity()
    {
        var entity = new TestEntity { Name = "ById" };
        SeedAsync(entity).GetAwaiter().GetResult();
        ReadOnlyRepository<TestEntity> repository = CreateRepository();

        TestEntity? found = repository.GetById(entity.Id);

        found.Should().NotBeNull();
        found!.Name.Should().Be("ById");
    }

    [DockerFact]
    public void SpecificationMethods_ShouldFilterEntities()
    {
        SeedAsync(new TestEntity { Name = "Active" }, new TestEntity { Name = "Inactive" }).GetAwaiter().GetResult();
        ReadOnlyRepository<TestEntity> repository = CreateRepository();
        var spec = new SyncActiveNameSpecification("Active");

        repository.AnyBySpecification(spec).Should().BeTrue();
        repository.CountBySpecification(spec).Should().Be(1);
        repository.GetBySpecification(spec).Should().ContainSingle();
        repository.GetSingleBySpecification(spec)!.Name.Should().Be("Active");
        repository.GetFirstBySpecification(spec)!.Name.Should().Be("Active");
    }

    [DockerFact]
    public void GetByKeysetPagination_ShouldReturnPages()
    {
        SeedAsync(
            new TestEntity { Name = "A" },
            new TestEntity { Name = "B" },
            new TestEntity { Name = "C" }).GetAwaiter().GetResult();
        ReadOnlyRepository<TestEntity> repository = CreateRepository();

        IKeysetPageResultString<TestEntity> firstPage = repository.GetByKeysetPagination(
            clause: null,
            keySelector: e => e.Name,
            lastKey: null,
            pageSize: 2,
            ascending: true);

        firstPage.Items.Should().HaveCount(2);
        firstPage.HasMore.Should().BeTrue();

        IKeysetPageResultString<TestEntity> secondPage = repository.GetByKeysetPagination(
            clause: null,
            keySelector: e => e.Name,
            lastKey: firstPage.LastKey,
            pageSize: 2,
            ascending: true);

        secondPage.Items.Should().NotBeEmpty();
    }

    [DockerFact]
    public void LoadRelation_ShouldThrowNotSupportedException()
    {
        ReadOnlyRepository<TestEntity> repository = CreateRepository();
        var entity = new TestEntity { Name = "Relation" };

        Action act = () => repository.LoadRelation(entity, e => e.Name);

        act.Should().Throw<NotSupportedException>();
    }

    [DockerFact]
    public void List_ShouldReturnAllEntities()
    {
        SeedAsync(new TestEntity { Name = "List-A" }, new TestEntity { Name = "List-B" }).GetAwaiter().GetResult();
        ReadOnlyRepository<TestEntity> repository = CreateRepository();

        IList<TestEntity> results = repository.List();

        results.Should().HaveCount(2);
        results.Select(e => e.Name).Should().BeEquivalentTo(["List-A", "List-B"]);
    }

    [DockerFact]
    public void List_WithPagingCriteria_ShouldReturnSubset()
    {
        SeedAsync(
            new TestEntity { Name = "Page-A" },
            new TestEntity { Name = "Page-B" },
            new TestEntity { Name = "Page-C" }).GetAwaiter().GetResult();
        ReadOnlyRepository<TestEntity> repository = CreateRepository();
        var paging = new PagingCriteria(limit: 2, offset: 0);

        IList<TestEntity> page = repository.List(paging);

        page.Should().HaveCount(2);
        repository.ListCount().Should().Be(3);
    }

    [DockerFact]
    public void List_WithPagingCriteria_WhenEmpty_ShouldReturnEmptyList()
    {
        SeedAsync().GetAwaiter().GetResult();
        ReadOnlyRepository<TestEntity> repository = CreateRepository();
        var paging = new PagingCriteria(limit: 10, offset: 0);

        IList<TestEntity> results = repository.List(paging);

        results.Should().BeEmpty();
        repository.ListCount().Should().Be(0);
    }

    [DockerFact]
    public void GetByKeysetPagination_WithSpecification_ShouldReturnFilteredPage()
    {
        SeedAsync(
            new TestEntity { Name = "Keep-A" },
            new TestEntity { Name = "Keep-B" },
            new TestEntity { Name = "Skip" }).GetAwaiter().GetResult();
        ReadOnlyRepository<TestEntity> repository = CreateRepository();
        var spec = new SyncNamePrefixSpecification("Keep");

        IKeysetPageResult<TestEntity, ObjectId> page = repository.GetByKeysetPagination(
            spec,
            keySelector: e => e.Id,
            lastKey: null,
            pageSize: 10,
            ascending: true);

        page.Items.Should().HaveCount(2);
        page.Items.Should().OnlyContain(e => e.Name.StartsWith("Keep", StringComparison.Ordinal));
    }

    [DockerFact]
    public void GetByKeysetPagination_WithStructKey_ShouldReturnPages()
    {
        SeedAsync(
            new TestEntity { Name = "Key-A" },
            new TestEntity { Name = "Key-B" },
            new TestEntity { Name = "Key-C" }).GetAwaiter().GetResult();
        ReadOnlyRepository<TestEntity> repository = CreateRepository();

        IKeysetPageResult<TestEntity, ObjectId> firstPage = repository.GetByKeysetPagination(
            clause: null,
            keySelector: e => e.Id,
            lastKey: null,
            pageSize: 2,
            ascending: true);

        firstPage.Items.Should().HaveCount(2);
        firstPage.HasMore.Should().BeTrue();

        IKeysetPageResult<TestEntity, ObjectId> secondPage = repository.GetByKeysetPagination(
            clause: null,
            keySelector: e => e.Id,
            lastKey: firstPage.LastKey,
            pageSize: 2,
            ascending: true);

        secondPage.Items.Should().NotBeEmpty();
    }

    [DockerFact]
    public void GetByKeysetPagination_Descending_ShouldReturnPagesInReverseOrder()
    {
        SeedAsync(
            new TestEntity { Name = "A" },
            new TestEntity { Name = "B" },
            new TestEntity { Name = "C" }).GetAwaiter().GetResult();
        ReadOnlyRepository<TestEntity> repository = CreateRepository();

        IKeysetPageResultString<TestEntity> firstPage = repository.GetByKeysetPagination(
            clause: null,
            keySelector: e => e.Name,
            lastKey: null,
            pageSize: 2,
            ascending: false);

        firstPage.Items.Should().HaveCount(2);
        firstPage.Items[0].Name.Should().Be("C");
        firstPage.Items[1].Name.Should().Be("B");
        firstPage.HasMore.Should().BeTrue();

        IKeysetPageResultString<TestEntity> secondPage = repository.GetByKeysetPagination(
            clause: null,
            keySelector: e => e.Name,
            lastKey: firstPage.LastKey,
            pageSize: 2,
            ascending: false);

        secondPage.Items.Should().ContainSingle();
        secondPage.Items[0].Name.Should().Be("A");
        secondPage.HasMore.Should().BeFalse();
    }

    [DockerFact]
    public void GetByKeysetPagination_WhenNoResults_ShouldReturnEmptyPage()
    {
        SeedAsync(new TestEntity { Name = "Only" }).GetAwaiter().GetResult();
        ReadOnlyRepository<TestEntity> repository = CreateRepository();

        IKeysetPageResultString<TestEntity> page = repository.GetByKeysetPagination(
            clause: e => e.Name == "Missing",
            keySelector: e => e.Name,
            lastKey: null,
            pageSize: 10,
            ascending: true);

        page.Items.Should().BeEmpty();
        page.HasMore.Should().BeFalse();
        page.LastKey.Should().BeNull();
    }

    [DockerFact]
    public void LoadRelation_WithCollectionExpression_ShouldThrowNotSupportedException()
    {
        ReadOnlyRepository<EntityWithRelations> repository = CreateRelationsRepository();
        var entity = new EntityWithRelations { Name = "Parent" };

        Action act = () => repository.LoadRelation(
            entity,
            e => e.Items,
            clause: item => item.Label == "child",
            limit: 1);

        act.Should().Throw<NotSupportedException>();
    }

    [DockerFact]
    public void LoadRelationSortByAscending_ShouldThrowNotSupportedException()
    {
        ReadOnlyRepository<EntityWithRelations> repository = CreateRelationsRepository();
        var entity = new EntityWithRelations { Name = "Parent" };

        Action act = () => repository.LoadRelationSortByAscending(
            entity,
            e => e.Items,
            item => item.Label,
            clause: item => item.Label.StartsWith("a", StringComparison.Ordinal),
            limit: 5);

        act.Should().Throw<NotSupportedException>();
    }

    [DockerFact]
    public void LoadRelationSortByDescending_ShouldThrowNotSupportedException()
    {
        ReadOnlyRepository<EntityWithRelations> repository = CreateRelationsRepository();
        var entity = new EntityWithRelations { Name = "Parent" };

        Action act = () => repository.LoadRelationSortByDescending(
            entity,
            e => e.Items,
            item => item.Label,
            clause: item => item.Label.StartsWith("z", StringComparison.Ordinal),
            limit: 5);

        act.Should().Throw<NotSupportedException>();
    }

    [DockerFact]
    public void GetBy_WithPagingCriteria_ShouldReturnFilteredPage()
    {
        SeedAsync(
            new TestEntity { Name = "Page-A" },
            new TestEntity { Name = "Page-B" },
            new TestEntity { Name = "Page-C" }).GetAwaiter().GetResult();
        ReadOnlyRepository<TestEntity> repository = CreateRepository();
        var paging = new PagingCriteria(limit: 1, offset: 1);

        IList<TestEntity> page = repository.GetBy(e => e.Name.StartsWith("Page"), paging);

        page.Should().ContainSingle();
        repository.GetByCount(e => e.Name.StartsWith("Page")).Should().Be(3);
    }

    [DockerFact]
    public void GetById_WithPagingCriteria_ShouldReturnEntity()
    {
        var entity = new TestEntity { Name = "PagedById" };
        SeedAsync(entity).GetAwaiter().GetResult();
        ReadOnlyRepository<TestEntity> repository = CreateRepository();
        var paging = new PagingCriteria(limit: 10, offset: 0);

        TestEntity? found = repository.GetById(entity.Id, paging);

        found.Should().NotBeNull();
        found!.Name.Should().Be("PagedById");
    }

    [DockerFact]
    public void GetByAny_WithNullClause_ShouldReturnTrueWhenDataExists()
    {
        SeedAsync(new TestEntity { Name = "Any" }).GetAwaiter().GetResult();
        ReadOnlyRepository<TestEntity> repository = CreateRepository();

        repository.GetByAny(null!).Should().BeTrue();
        repository.GetByCount(null!).Should().Be(1);
        repository.GetBy(null!).Should().ContainSingle();
    }

    [DockerFact]
    public void EntityLogBy_ShouldThrowNotSupportedException()
    {
        ReadOnlyRepository<TestEntity> repository = CreateRepository();

        Action act = () => _ = repository.GetType()
            .GetProperty("EntityLogBy", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(repository);

        act.Should().Throw<System.Reflection.TargetInvocationException>()
            .WithInnerException<NotSupportedException>();
    }

    [DockerFact]
    public void GetByKeysetPagination_WithClause_ShouldFilterBeforePaging()
    {
        SeedAsync(
            new TestEntity { Name = "Match-1" },
            new TestEntity { Name = "Match-2" },
            new TestEntity { Name = "Skip" }).GetAwaiter().GetResult();
        ReadOnlyRepository<TestEntity> repository = CreateRepository();

        IKeysetPageResultString<TestEntity> page = repository.GetByKeysetPagination(
            clause: e => e.Name.StartsWith("Match"),
            keySelector: e => e.Name,
            lastKey: null,
            pageSize: 10,
            ascending: true);

        page.Items.Should().HaveCount(2);
        page.HasMore.Should().BeFalse();
    }

    [DockerFact]
    public void GetByKeysetPagination_StructDescending_ShouldReturnReversePages()
    {
        SeedAsync(
            new TestEntity { Name = "A" },
            new TestEntity { Name = "B" },
            new TestEntity { Name = "C" }).GetAwaiter().GetResult();
        ReadOnlyRepository<TestEntity> repository = CreateRepository();

        IKeysetPageResult<TestEntity, ObjectId> firstPage = repository.GetByKeysetPagination(
            clause: null,
            keySelector: e => e.Id,
            lastKey: null,
            pageSize: 2,
            ascending: false);

        firstPage.Items.Should().HaveCount(2);
        firstPage.HasMore.Should().BeTrue();
    }

    private ReadOnlyRepository<EntityWithRelations> CreateRelationsRepository()
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture);
        return new ReadOnlyRepository<EntityWithRelations>(context, MongoDbIntegrationTestHelper.CreateRepositoryOptions());
    }
}

internal sealed class SyncActiveNameSpecification(string name) : Specification<TestEntity>
{
    protected override Expression<Func<TestEntity, bool>> Criteria => entity => entity.Name == name;
}

internal sealed class SyncNamePrefixSpecification(string prefix) : Specification<TestEntity>
{
    protected override Expression<Func<TestEntity, bool>> Criteria => entity => entity.Name.StartsWith(prefix);
}

internal sealed class RelatedItem
{
    public string Label { get; set; } = string.Empty;
}

internal sealed class EntityWithRelations : IEntityBase
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    public string Name { get; set; } = string.Empty;

    public List<RelatedItem> Items { get; set; } = [];

    public object EntityKey => Id;

    public IReadOnlyCollection<MessageResult> GetNotifications()
    {
        return [];
    }

    public bool HasNotifications()
    {
        return false;
    }
}
