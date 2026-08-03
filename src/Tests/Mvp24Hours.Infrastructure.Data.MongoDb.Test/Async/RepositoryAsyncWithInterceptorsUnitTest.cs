using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Data.MongoDb;
using Mvp24Hours.Infrastructure.Data.MongoDb.Interceptors;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Async;

[Trait("Category", "Unit")]
public class RepositoryAsyncWithInterceptorsUnitTest
{
    private static TestableRepositoryAsyncWithInterceptors CreateRepository(
        out Mock<IMongoDbInterceptorPipeline> pipelineMock,
        out Mock<IMongoCollection<TestEntity>> collectionMock)
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();
        pipelineMock = new Mock<IMongoDbInterceptorPipeline>();
        collectionMock = new Mock<IMongoCollection<TestEntity>>();
        var repository = new TestableRepositoryAsyncWithInterceptors(
            context,
            MongoDbTestContextFactory.CreateRepositoryOptions(),
            pipelineMock.Object);
        repository.SetCollection(collectionMock.Object);
        return repository;
    }

    [Fact]
    public async Task AddAsync_WithNullEntity_ShouldNotInvokePipeline()
    {
        TestableRepositoryAsyncWithInterceptors repository = CreateRepository(out Mock<IMongoDbInterceptorPipeline> pipelineMock, out _);

        await repository.AddAsync((TestEntity)null!);

        pipelineMock.Verify(
            p => p.ExecuteInsertAsync(
                It.IsAny<TestEntity>(),
                It.IsAny<Func<Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddAsync_WithEntity_ShouldInvokePipeline()
    {
        TestableRepositoryAsyncWithInterceptors repository = CreateRepository(out Mock<IMongoDbInterceptorPipeline> pipelineMock, out _);
        var entity = new TestEntity { Name = "Add" };
        pipelineMock
            .Setup(p => p.ExecuteInsertAsync(entity, It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<TestEntity, Func<Task>, CancellationToken>((_, op, _) => op());

        await repository.AddAsync(entity);

        pipelineMock.Verify(
            p => p.ExecuteInsertAsync(entity, It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddAsync_WithEmptyList_ShouldNotInvokePipeline()
    {
        TestableRepositoryAsyncWithInterceptors repository = CreateRepository(out Mock<IMongoDbInterceptorPipeline> pipelineMock, out _);

        await repository.AddAsync([]);

        pipelineMock.Verify(
            p => p.ExecuteInsertAsync(
                It.IsAny<TestEntity>(),
                It.IsAny<Func<Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ModifyAsync_WithNullEntity_ShouldNotInvokePipeline()
    {
        TestableRepositoryAsyncWithInterceptors repository = CreateRepository(out Mock<IMongoDbInterceptorPipeline> pipelineMock, out _);

        await repository.ModifyAsync((TestEntity)null!);

        pipelineMock.Verify(
            p => p.ExecuteUpdateAsync(
                It.IsAny<TestEntity>(),
                It.IsAny<Func<Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RemoveAsync_WithNullEntity_ShouldNotInvokePipeline()
    {
        TestableRepositoryAsyncWithInterceptors repository = CreateRepository(out Mock<IMongoDbInterceptorPipeline> pipelineMock, out _);

        await repository.RemoveAsync((TestEntity)null!);

        pipelineMock.Verify(
            p => p.ExecuteDeleteAsync(
                It.IsAny<TestEntity>(),
                It.IsAny<Func<Task>>(),
                It.IsAny<Func<Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RemoveAsync_WithHardDelete_ShouldInvokeHardDeleteOperation()
    {
        TestableRepositoryAsyncWithInterceptors repository = CreateRepository(out Mock<IMongoDbInterceptorPipeline> pipelineMock, out Mock<IMongoCollection<TestEntity>> collectionMock);
        var entity = new TestEntity { Name = "Delete" };
        pipelineMock
            .Setup(p => p.ExecuteDeleteAsync(
                entity,
                It.IsAny<Func<Task>>(),
                It.IsAny<Func<Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns<TestEntity, Func<Task>, Func<Task>, CancellationToken>((_, hardDelete, _, _) => hardDelete().ContinueWith(_ => false));
        collectionMock
            .Setup(c => c.DeleteOneAsync(
                It.IsAny<FilterDefinition<TestEntity>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteResult.Acknowledged(1));

        await repository.RemoveAsync(entity);

        collectionMock.Verify(
            c => c.DeleteOneAsync(It.IsAny<FilterDefinition<TestEntity>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LoadRelationAsync_ShouldThrowNotSupportedException()
    {
        TestableRepositoryAsyncWithInterceptors repository = CreateRepository(out _, out _);
        var entity = new TestEntity { Name = "Relation" };

        Func<Task> act = () => repository.LoadRelationAsync(entity, e => e.Name);

        await act.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task LoadRelationSortByDescendingAsync_ShouldThrowNotSupportedException()
    {
        var repository = new RepositoryAsyncWithInterceptors<InterceptorEntityWithRelations>(
            MongoDbTestContextFactory.Create(),
            MongoDbTestContextFactory.CreateRepositoryOptions());
        var entity = new InterceptorEntityWithRelations { Name = "Parent" };

        Func<Task> act = () => repository.LoadRelationSortByDescendingAsync(
            entity,
            e => e.Items,
            item => item.Label);

        await act.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public void EntityLogBy_ShouldThrowNotSupportedException()
    {
        TestableRepositoryAsyncWithInterceptors repository = CreateRepository(out _, out _);

        Action act = () => _ = repository.GetType()
            .GetProperty("EntityLogBy", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(repository);

        act.Should().Throw<System.Reflection.TargetInvocationException>()
            .WithInnerException<NotSupportedException>();
    }
}

internal sealed class InterceptorEntityWithRelations : IEntityBase
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

internal sealed class RelatedItem
{
    public string Label { get; set; } = string.Empty;
}
