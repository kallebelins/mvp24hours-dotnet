using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test;

[Trait("Category", "Unit")]
public class ReadOnlyRepositoryUnitTest
{
    private static TestableReadOnlyRepository CreateRepository()
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();
        var repository = new TestableReadOnlyRepository(context, MongoDbTestContextFactory.CreateRepositoryOptions());
        repository.SetCollection(new Moq.Mock<MongoDB.Driver.IMongoCollection<TestEntity>>().Object);
        return repository;
    }

    [Fact]
    public void LoadRelation_SingleProperty_ShouldThrowNotSupportedException()
    {
        TestableReadOnlyRepository repository = CreateRepository();
        var entity = new TestEntity { Name = "Relation" };

        Action act = () => repository.LoadRelation(entity, e => e.Name);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void LoadRelation_Collection_ShouldThrowNotSupportedException()
    {
        var repository = new ReadOnlyRepository<ReadOnlyEntityWithRelations>(
            MongoDbTestContextFactory.Create(),
            MongoDbTestContextFactory.CreateRepositoryOptions());
        var entity = new ReadOnlyEntityWithRelations { Name = "Parent" };

        Action act = () => repository.LoadRelation(entity, e => e.Items);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void LoadRelationSortByAscending_ShouldThrowNotSupportedException()
    {
        var repository = new ReadOnlyRepository<ReadOnlyEntityWithRelations>(
            MongoDbTestContextFactory.Create(),
            MongoDbTestContextFactory.CreateRepositoryOptions());
        var entity = new ReadOnlyEntityWithRelations { Name = "Parent" };

        Action act = () => repository.LoadRelationSortByAscending(entity, e => e.Items, item => item.Label);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void LoadRelationSortByDescending_ShouldThrowNotSupportedException()
    {
        var repository = new ReadOnlyRepository<ReadOnlyEntityWithRelations>(
            MongoDbTestContextFactory.Create(),
            MongoDbTestContextFactory.CreateRepositoryOptions());
        var entity = new ReadOnlyEntityWithRelations { Name = "Parent" };

        Action act = () => repository.LoadRelationSortByDescending(entity, e => e.Items, item => item.Label);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void EntityLogBy_ShouldThrowNotSupportedException()
    {
        // ReadOnlyRepository<T> never performs writes, so EntityLogBy (used only by
        // Modify/Remove soft-delete stamping in Repository<T>) is intentionally unsupported.
        TestableReadOnlyRepository repository = CreateRepository();

        Action act = () => _ = repository.GetType()
            .GetProperty("EntityLogBy", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(repository);

        act.Should().Throw<System.Reflection.TargetInvocationException>()
            .WithInnerException<NotSupportedException>();
    }
}

internal sealed class ReadOnlyEntityWithRelations : Mvp24Hours.Core.Contract.Domain.Entity.IEntityBase
{
    public MongoDB.Bson.ObjectId Id { get; set; } = MongoDB.Bson.ObjectId.GenerateNewId();

    public string Name { get; set; } = string.Empty;

    public List<ReadOnlyRelatedItem> Items { get; set; } = [];

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

internal sealed class ReadOnlyRelatedItem
{
    public string Label { get; set; } = string.Empty;
}
