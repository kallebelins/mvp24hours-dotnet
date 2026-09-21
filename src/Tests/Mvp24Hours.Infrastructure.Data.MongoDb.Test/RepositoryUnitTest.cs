using MongoDB.Driver;
using Moq;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test;

[Trait("Category", "Unit")]
public class RepositoryUnitTest
{
    private static TestableRepository CreateRepository(out Mock<IMongoCollection<TestEntity>> collectionMock)
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();
        var repository = new TestableRepository(context, MongoDbTestContextFactory.CreateRepositoryOptions());
        collectionMock = new Mock<IMongoCollection<TestEntity>>();
        repository.SetCollection(collectionMock.Object);
        return repository;
    }

    [Fact]
    public void Add_WithNullEntity_ShouldNotInsert()
    {
        TestableRepository repository = CreateRepository(out Mock<IMongoCollection<TestEntity>> collectionMock);

        repository.Add((TestEntity)null!);

        collectionMock.Verify(c => c.InsertOne(It.IsAny<TestEntity>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Add_WithEntity_ShouldInsertOne()
    {
        TestableRepository repository = CreateRepository(out Mock<IMongoCollection<TestEntity>> collectionMock);
        var entity = new TestEntity { Name = "Add" };

        repository.Add(entity);

        collectionMock.Verify(c => c.InsertOne(entity, null, default), Times.Once);
    }

    [Fact]
    public void Add_WithEmptyList_ShouldNotInsertMany()
    {
        TestableRepository repository = CreateRepository(out Mock<IMongoCollection<TestEntity>> collectionMock);

        repository.Add(new List<TestEntity>());

        collectionMock.Verify(c => c.InsertMany(It.IsAny<IEnumerable<TestEntity>>(), It.IsAny<InsertManyOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Add_WithListContainingNulls_ShouldOnlyInsertNonNullEntities()
    {
        TestableRepository repository = CreateRepository(out Mock<IMongoCollection<TestEntity>> collectionMock);
        var entity = new TestEntity { Name = "Keep" };
        IList<TestEntity> entities = [entity, null!];

        repository.Add(entities);

        collectionMock.Verify(c => c.InsertMany(
            It.Is<IEnumerable<TestEntity>>(list => list.Count() == 1 && list.Single() == entity),
            null,
            default),
            Times.Once);
    }

    [Fact]
    public void Add_WithListOfAllNulls_ShouldNotInsertMany()
    {
        TestableRepository repository = CreateRepository(out Mock<IMongoCollection<TestEntity>> collectionMock);
        IList<TestEntity> entities = [null!, null!];

        repository.Add(entities);

        collectionMock.Verify(c => c.InsertMany(It.IsAny<IEnumerable<TestEntity>>(), It.IsAny<InsertManyOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Modify_WithNullEntity_ShouldReturnWithoutTouchingCollection()
    {
        TestableRepository repository = CreateRepository(out Mock<IMongoCollection<TestEntity>> collectionMock);

        repository.Modify((TestEntity)null!);

        collectionMock.Verify(c => c.ReplaceOne(
            It.IsAny<FilterDefinition<TestEntity>>(),
            It.IsAny<TestEntity>(),
            It.IsAny<ReplaceOptions>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void Modify_WithEmptyList_ShouldNotIterate()
    {
        TestableRepository repository = CreateRepository(out Mock<IMongoCollection<TestEntity>> collectionMock);

        repository.Modify(new List<TestEntity>());

        collectionMock.Verify(c => c.ReplaceOne(
            It.IsAny<FilterDefinition<TestEntity>>(),
            It.IsAny<TestEntity>(),
            It.IsAny<ReplaceOptions>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void Remove_WithNullEntity_ShouldReturnWithoutTouchingCollection()
    {
        TestableRepository repository = CreateRepository(out Mock<IMongoCollection<TestEntity>> collectionMock);

        repository.Remove((TestEntity)null!);

        collectionMock.Verify(c => c.DeleteOne(It.IsAny<FilterDefinition<TestEntity>>(), It.IsAny<CancellationToken>()), Times.Never);
        collectionMock.Verify(c => c.ReplaceOne(
            It.IsAny<FilterDefinition<TestEntity>>(),
            It.IsAny<TestEntity>(),
            It.IsAny<ReplaceOptions>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void Remove_WithEmptyList_ShouldNotIterate()
    {
        TestableRepository repository = CreateRepository(out Mock<IMongoCollection<TestEntity>> collectionMock);

        repository.Remove(new List<TestEntity>());

        collectionMock.Verify(c => c.DeleteOne(It.IsAny<FilterDefinition<TestEntity>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Remove_WithoutEntityDateLog_ShouldHardDeleteViaDeleteOne()
    {
        // TestEntity implements only IEntityBase (no IEntityDateLog), so Remove must
        // go through ForceRemove -> DeleteOne rather than the soft-delete Modify path.
        TestableRepository repository = CreateRepository(out Mock<IMongoCollection<TestEntity>> collectionMock);
        var entity = new TestEntity { Name = "HardDelete" };

        repository.Remove(entity);

        collectionMock.Verify(c => c.DeleteOne(It.IsAny<FilterDefinition<TestEntity>>(), default), Times.Once);
    }

    [Fact]
    public void RemoveById_WithEmptyIdList_ShouldNotIterate()
    {
        TestableRepository repository = CreateRepository(out Mock<IMongoCollection<TestEntity>> collectionMock);

        repository.RemoveById(new List<object>());

        collectionMock.Verify(c => c.DeleteOne(It.IsAny<FilterDefinition<TestEntity>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void LoadRelation_SingleProperty_ShouldThrowNotSupportedException()
    {
        TestableRepository repository = CreateRepository(out _);
        var entity = new TestEntity { Name = "Relation" };

        Action act = () => repository.LoadRelation(entity, e => e.Name);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void LoadRelation_Collection_ShouldThrowNotSupportedException()
    {
        var repository = new Repository<RepoEntityWithRelations>(
            MongoDbTestContextFactory.Create(),
            MongoDbTestContextFactory.CreateRepositoryOptions());
        var entity = new RepoEntityWithRelations { Name = "Parent" };

        Action act = () => repository.LoadRelation(entity, e => e.Items);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void LoadRelationSortByAscending_ShouldThrowNotSupportedException()
    {
        var repository = new Repository<RepoEntityWithRelations>(
            MongoDbTestContextFactory.Create(),
            MongoDbTestContextFactory.CreateRepositoryOptions());
        var entity = new RepoEntityWithRelations { Name = "Parent" };

        Action act = () => repository.LoadRelationSortByAscending(entity, e => e.Items, item => item.Label);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void LoadRelationSortByDescending_ShouldThrowNotSupportedException()
    {
        var repository = new Repository<RepoEntityWithRelations>(
            MongoDbTestContextFactory.Create(),
            MongoDbTestContextFactory.CreateRepositoryOptions());
        var entity = new RepoEntityWithRelations { Name = "Parent" };

        Action act = () => repository.LoadRelationSortByDescending(entity, e => e.Items, item => item.Label);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void EntityLogBy_ShouldBeNull()
    {
        // Repository<T> never tracks a current user by itself; RemovedBy population on soft
        // delete is delegated entirely to RepositoryAsyncWithInterceptors + AuditInterceptor.
        TestableRepository repository = CreateRepository(out _);

        object? entityLogBy = repository.GetType()
            .GetProperty("EntityLogBy", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(repository);

        entityLogBy.Should().BeNull();
    }
}

internal sealed class RepoEntityWithRelations : Mvp24Hours.Core.Contract.Domain.Entity.IEntityBase
{
    public MongoDB.Bson.ObjectId Id { get; set; } = MongoDB.Bson.ObjectId.GenerateNewId();

    public string Name { get; set; } = string.Empty;

    public List<RepoRelatedItem> Items { get; set; } = [];

    public object EntityKey => Id;

    public IReadOnlyCollection<Mvp24Hours.Core.ValueObjects.Logic.MessageResult> GetNotifications()
    {
        return [];
    }

    public bool HasNotifications()
    {
        return false;
    }
}

internal sealed class RepoRelatedItem
{
    public string Label { get; set; } = string.Empty;
}
