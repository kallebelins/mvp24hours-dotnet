using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using Moq;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Data.MongoDb;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using Mvp24Hours.Infrastructure.Data.MongoDb.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Async;

[Trait("Category", "Unit")]
public class BulkOperationsRepositoryAsyncUnitTest
{
    private static TestableBulkOperationsRepositoryAsync CreateRepository(out Mock<IMongoCollection<TestEntity>> collectionMock)
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();
        var mock = new Mock<IMongoCollection<TestEntity>>();
        var repository = new TestableBulkOperationsRepositoryAsync(context, MongoDbTestContextFactory.CreateRepositoryOptions());
        repository.SetCollection(mock.Object);
        collectionMock = mock;
        return repository;
    }

    [Fact]
    public async Task BulkInsertAsync_WithEmptyList_ShouldReturnZeroRows()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out _);

        BulkOperationResult result = await repository.BulkInsertAsync([]);

        result.IsSuccess.Should().BeTrue();
        result.RowsAffected.Should().Be(0);
    }

    [Fact]
    public async Task BulkInsertAsync_WithNullMongoOptions_ShouldThrow()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out _);

        Func<Task> act = () => repository.BulkInsertAsync([new TestEntity()], (MongoDbBulkOperationOptions)null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task BulkUpdateAsync_WithNullMongoOptions_ShouldThrow()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out _);

        Func<Task> act = () => repository.BulkUpdateAsync([new TestEntity()], (MongoDbBulkOperationOptions)null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task BulkDeleteAsync_WithNullMongoOptions_ShouldThrow()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out _);

        Func<Task> act = () => repository.BulkDeleteAsync([new TestEntity()], (MongoDbBulkOperationOptions)null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task BulkWriteAsync_WithNullRequests_ShouldThrow()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out _);

        Func<Task> act = () => repository.BulkWriteAsync(null!, MongoDbBulkOperationOptions.Default);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteUpdateAsync_WithNullPredicate_ShouldThrow()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out _);

        Func<Task> act = () => repository.ExecuteUpdateAsync(null!, e => e.Name, "x");

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteUpdateAsync_WithNullProperty_ShouldThrow()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out _);

        Func<Task> act = () => repository.ExecuteUpdateAsync(_ => true, null!, "x");

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteUpdateAsync_WithNullSetPropertyCalls_ShouldThrow()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out _);

        Func<Task> act = () => repository.ExecuteUpdateAsync(_ => true, null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteDeleteAsync_WithNullPredicate_ShouldThrow()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out _);

        Func<Task> act = () => repository.ExecuteDeleteAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateManyAsync_WithNullFilterExpression_ShouldThrow()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out _);

        Func<Task> act = () => repository.UpdateManyAsync(null!, Builders<TestEntity>.Update.Set(e => e.Name, "x"));

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateManyAsync_WithNullUpdate_ShouldThrow()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out _);

        Func<Task> act = () => repository.UpdateManyAsync(_ => true, null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateManyAsync_WithNullFilterDefinition_ShouldThrow()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out _);

        Func<Task> act = () => repository.UpdateManyAsync(null!, Builders<TestEntity>.Update.Set(e => e.Name, "x"));

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DeleteManyAsync_WithNullFilterExpression_ShouldThrow()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out _);

        Func<Task> act = () => repository.DeleteManyAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DeleteManyAsync_WithNullFilterDefinition_ShouldThrow()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out _);

        Func<Task> act = () => repository.DeleteManyAsync((FilterDefinition<TestEntity>)null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task BulkInsertAsync_WhenInsertThrows_ShouldReturnFailure()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out Mock<IMongoCollection<TestEntity>> collectionMock);
        collectionMock
            .Setup(c => c.InsertManyAsync(
                It.IsAny<IEnumerable<TestEntity>>(),
                It.IsAny<InsertManyOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("insert failed"));

        BulkOperationResult result = await repository.BulkInsertAsync([new TestEntity { Name = "Fail" }]);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("insert failed");
    }

    [Fact]
    public async Task BulkUpdateAsync_WhenBulkWriteThrows_ShouldReturnFailure()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out Mock<IMongoCollection<TestEntity>> collectionMock);
        collectionMock
            .Setup(c => c.BulkWriteAsync(
                It.IsAny<IEnumerable<WriteModel<TestEntity>>>(),
                It.IsAny<BulkWriteOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("update failed"));

        BulkOperationResult result = await repository.BulkUpdateAsync([new TestEntity { Name = "Fail" }]);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("update failed");
    }

    [Fact]
    public async Task BulkDeleteAsync_WhenBulkWriteThrows_ShouldReturnFailure()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out Mock<IMongoCollection<TestEntity>> collectionMock);
        collectionMock
            .Setup(c => c.BulkWriteAsync(
                It.IsAny<IEnumerable<WriteModel<TestEntity>>>(),
                It.IsAny<BulkWriteOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("delete failed"));

        BulkOperationResult result = await repository.BulkDeleteAsync([new TestEntity { Name = "Fail" }]);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("delete failed");
    }

    [Fact]
    public async Task BulkWriteAsync_WhenBulkWriteThrows_ShouldReturnFailure()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out Mock<IMongoCollection<TestEntity>> collectionMock);
        collectionMock
            .Setup(c => c.BulkWriteAsync(
                It.IsAny<IEnumerable<WriteModel<TestEntity>>>(),
                It.IsAny<BulkWriteOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("bulk write failed"));

        MongoDbBulkOperationResult result = await repository.BulkWriteAsync(
            [new InsertOneModel<TestEntity>(new TestEntity { Name = "Fail" })],
            MongoDbBulkOperationOptions.Default);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("bulk write failed");
    }

    [Fact]
    public async Task ExecuteUpdateAsync_WhenUpdateManyThrows_ShouldRethrow()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out Mock<IMongoCollection<TestEntity>> collectionMock);
        collectionMock
            .Setup(c => c.UpdateManyAsync(
                It.IsAny<FilterDefinition<TestEntity>>(),
                It.IsAny<UpdateDefinition<TestEntity>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("execute update failed"));

        Func<Task> act = () => repository.ExecuteUpdateAsync(_ => true, e => e.Name, "updated");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("execute update failed");
    }

    [Fact]
    public async Task ExecuteDeleteAsync_WhenDeleteManyThrows_ShouldRethrow()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out Mock<IMongoCollection<TestEntity>> collectionMock);
        collectionMock
            .Setup(c => c.DeleteManyAsync(
                It.IsAny<FilterDefinition<TestEntity>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("execute delete failed"));

        Func<Task> act = () => repository.ExecuteDeleteAsync(_ => true);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("execute delete failed");
    }

    [Fact]
    public async Task ExecuteUpdateAsync_WithEmptySetters_ShouldReturnZero()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out _);

        int rowsAffected = await repository.ExecuteUpdateAsync(_ => true, s => s);

        rowsAffected.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteUpdateAsync_WithExpressionBasedSetter_ShouldThrowNotSupportedException()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out _);

        Func<Task> act = () => repository.ExecuteUpdateAsync(
            _ => true,
            s => s.SetProperty(e => e.Name, e => e.Name + "-copy"));

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*Expression-based property setters are not supported*");
    }

    [Fact]
    public async Task BulkInsertAsync_WithCancellationRequested_ShouldReturnFailure()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out _);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        BulkOperationResult result = await repository.BulkInsertAsync(
            [new TestEntity { Name = "Cancel" }],
            new MongoDbBulkOperationOptions(),
            cts.Token);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task BulkInsertAsync_WithBulkOperationOptionsOverload_ShouldConvertOptions()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out Mock<IMongoCollection<TestEntity>> collectionMock);
        collectionMock
            .Setup(c => c.InsertManyAsync(
                It.IsAny<IEnumerable<TestEntity>>(),
                It.IsAny<InsertManyOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        BulkOperationResult result = await repository.BulkInsertAsync(
            [new TestEntity { Name = "Options" }],
            new BulkOperationOptions { BatchSize = 1 });

        result.IsSuccess.Should().BeTrue();
        result.RowsAffected.Should().Be(1);
    }

    [Fact]
    public async Task BulkInsertAsync_WithLogger_ShouldCompleteSuccessfully()
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();
        var mock = new Mock<IMongoCollection<TestEntity>>();
        mock.Setup(c => c.InsertManyAsync(
                It.IsAny<IEnumerable<TestEntity>>(),
                It.IsAny<InsertManyOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var testable = new TestableBulkOperationsRepositoryAsync(context, MongoDbTestContextFactory.CreateRepositoryOptions());
        testable.SetCollection(mock.Object);

        BulkOperationResult result = await testable.BulkInsertAsync([new TestEntity { Name = "Logged" }]);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateManyAsync_WithExpression_ShouldReturnModifiedCount()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out Mock<IMongoCollection<TestEntity>> collectionMock);
        collectionMock
            .Setup(c => c.UpdateManyAsync(
                It.IsAny<FilterDefinition<TestEntity>>(),
                It.IsAny<UpdateDefinition<TestEntity>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(3, 3, null));

        long modified = await repository.UpdateManyAsync(_ => true, Builders<TestEntity>.Update.Set(e => e.Name, "x"));

        modified.Should().Be(3);
    }

    [Fact]
    public async Task UpdateManyAsync_WithFilterDefinition_ShouldReturnModifiedCount()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out Mock<IMongoCollection<TestEntity>> collectionMock);
        collectionMock
            .Setup(c => c.UpdateManyAsync(
                It.IsAny<FilterDefinition<TestEntity>>(),
                It.IsAny<UpdateDefinition<TestEntity>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(2, 2, null));

        long modified = await repository.UpdateManyAsync(
            Builders<TestEntity>.Filter.Eq(e => e.Name, "x"),
            Builders<TestEntity>.Update.Set(e => e.Name, "y"));

        modified.Should().Be(2);
    }

    [Fact]
    public async Task DeleteManyAsync_WithExpression_ShouldReturnDeletedCount()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out Mock<IMongoCollection<TestEntity>> collectionMock);
        collectionMock
            .Setup(c => c.DeleteManyAsync(
                It.IsAny<FilterDefinition<TestEntity>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteResult.Acknowledged(4));

        long deleted = await repository.DeleteManyAsync(_ => true);

        deleted.Should().Be(4);
    }

    [Fact]
    public async Task DeleteManyAsync_WithFilterDefinition_ShouldReturnDeletedCount()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out Mock<IMongoCollection<TestEntity>> collectionMock);
        collectionMock
            .Setup(c => c.DeleteManyAsync(
                It.IsAny<FilterDefinition<TestEntity>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteResult.Acknowledged(1));

        long deleted = await repository.DeleteManyAsync(Builders<TestEntity>.Filter.Empty);

        deleted.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteUpdateAsync_WithMultipleSetters_ShouldReturnModifiedCount()
    {
        TestableBulkOperationsRepositoryAsync repository = CreateRepository(out Mock<IMongoCollection<TestEntity>> collectionMock);
        collectionMock
            .Setup(c => c.UpdateManyAsync(
                It.IsAny<FilterDefinition<TestEntity>>(),
                It.IsAny<UpdateDefinition<TestEntity>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(5, 5, null));

        int rows = await repository.ExecuteUpdateAsync(
            _ => true,
            s => s.SetProperty(e => e.Name, "multi").SetProperty(e => e.Name, "multi2"));

        rows.Should().Be(5);
    }
}
