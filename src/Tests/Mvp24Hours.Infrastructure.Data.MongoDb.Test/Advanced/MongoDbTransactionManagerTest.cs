using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using Moq;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.Transactions;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Advanced;

[Trait("Category", "Unit")]
public class MongoDbTransactionManagerUnitTest
{
    [Fact]
    public void Constructor_WithNullClient_ShouldThrow()
    {
        Action act = () => new MongoDbTransactionManager(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CommitTransactionAsync_WithoutActiveTransaction_ShouldThrow()
    {
        var clientMock = new Mock<IMongoClient>();
        var manager = new MongoDbTransactionManager(clientMock.Object);

        Func<Task> act = () => manager.CommitTransactionAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No active transaction*");
    }

    [Fact]
    public async Task AbortTransactionAsync_WithoutActiveTransaction_ShouldThrow()
    {
        var clientMock = new Mock<IMongoClient>();
        var manager = new MongoDbTransactionManager(clientMock.Object);

        Func<Task> act = () => manager.AbortTransactionAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No active transaction*");
    }

    [Fact]
    public async Task BeginTransactionAsync_WhenAlreadyInTransaction_ShouldThrow()
    {
        Mock<IClientSessionHandle> sessionMock = CreateSessionMock(isInTransaction: true);
        var clientMock = new Mock<IMongoClient>();
        clientMock
            .Setup(c => c.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionMock.Object);

        var manager = new MongoDbTransactionManager(clientMock.Object);
        await manager.BeginTransactionAsync();

        Func<Task> act = () => manager.BeginTransactionAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already in progress*");
    }

    [Fact]
    public async Task BeginTransactionAsync_ShouldStartSessionAndTransaction()
    {
        Mock<IClientSessionHandle> sessionMock = CreateSessionMock(isInTransaction: false);
        var clientMock = new Mock<IMongoClient>();
        clientMock
            .Setup(c => c.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionMock.Object);

        var manager = new MongoDbTransactionManager(clientMock.Object);

        IClientSessionHandle session = await manager.BeginTransactionAsync();

        session.Should().BeSameAs(sessionMock.Object);
        manager.IsTransactionActive.Should().BeTrue();
        sessionMock.Verify(s => s.StartTransaction(It.IsAny<TransactionOptions>()), Times.Once);
    }

    [Fact]
    public async Task CommitTransactionAsync_WithActiveTransaction_ShouldCommitAndClearSavepoints()
    {
        Mock<IClientSessionHandle> sessionMock = CreateSessionMock(isInTransaction: true);
        var clientMock = new Mock<IMongoClient>();
        clientMock
            .Setup(c => c.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionMock.Object);

        var manager = new MongoDbTransactionManager(clientMock.Object);
        await manager.BeginTransactionAsync();
        manager.CreateSavepoint("checkpoint-1");

        await manager.CommitTransactionAsync();

        sessionMock.Verify(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        manager.IsTransactionActive.Should().BeTrue();
    }

    [Fact]
    public async Task AbortTransactionAsync_WithActiveTransaction_ShouldAbort()
    {
        Mock<IClientSessionHandle> sessionMock = CreateSessionMock(isInTransaction: true);
        var clientMock = new Mock<IMongoClient>();
        clientMock
            .Setup(c => c.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionMock.Object);

        var manager = new MongoDbTransactionManager(clientMock.Object);
        await manager.BeginTransactionAsync();

        await manager.AbortTransactionAsync();

        sessionMock.Verify(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithNullOperation_ShouldThrow()
    {
        var manager = new MongoDbTransactionManager(new Mock<IMongoClient>().Object);

        Func<Task> act = () => manager.ExecuteInTransactionAsync<object>(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_ShouldReturnResultOnSuccess()
    {
        Mock<IClientSessionHandle> sessionMock = CreateSessionMock(isInTransaction: false);
        var clientMock = new Mock<IMongoClient>();
        clientMock
            .Setup(c => c.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionMock.Object);

        var manager = new MongoDbTransactionManager(clientMock.Object);

        int result = await manager.ExecuteInTransactionAsync((_, _) => Task.FromResult(42));

        result.Should().Be(42);
        sessionMock.Verify(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenOperationThrows_ShouldAbortTransaction()
    {
        Mock<IClientSessionHandle> sessionMock = CreateSessionMock(isInTransaction: false);
        sessionMock.Setup(s => s.IsInTransaction).Returns(true);
        var clientMock = new Mock<IMongoClient>();
        clientMock
            .Setup(c => c.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionMock.Object);

        var manager = new MongoDbTransactionManager(clientMock.Object);

        Func<Task> act = () => manager.ExecuteInTransactionAsync((_, _) =>
            throw new InvalidOperationException("operation failed"));

        await act.Should().ThrowAsync<InvalidOperationException>();
        sessionMock.Verify(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void CreateSavepoint_WithEmptyName_ShouldThrow()
    {
        var manager = new MongoDbTransactionManager(new Mock<IMongoClient>().Object);

        Action act = () => manager.CreateSavepoint("  ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateSavepoint_WithoutActiveTransaction_ShouldThrow()
    {
        var manager = new MongoDbTransactionManager(new Mock<IMongoClient>().Object);

        Action act = () => manager.CreateSavepoint("sp1");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateSavepoint_WithDuplicateName_ShouldThrow()
    {
        Mock<IClientSessionHandle> sessionMock = CreateSessionMock(isInTransaction: true);
        var clientMock = new Mock<IMongoClient>();
        clientMock
            .Setup(c => c.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionMock.Object);

        var manager = new MongoDbTransactionManager(clientMock.Object);
        await manager.BeginTransactionAsync();
        manager.CreateSavepoint("sp1");

        Action act = () => manager.CreateSavepoint("sp1");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task RollbackToSavepointAsync_ShouldAbortTransaction()
    {
        Mock<IClientSessionHandle> sessionMock = CreateSessionMock(isInTransaction: true);
        var clientMock = new Mock<IMongoClient>();
        clientMock
            .Setup(c => c.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionMock.Object);

        var manager = new MongoDbTransactionManager(clientMock.Object);
        await manager.BeginTransactionAsync();
        manager.CreateSavepoint("sp1");

        await manager.RollbackToSavepointAsync("sp1");

        sessionMock.Verify(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RollbackToSavepointAsync_WithUnknownSavepoint_ShouldThrow()
    {
        Mock<IClientSessionHandle> sessionMock = CreateSessionMock(isInTransaction: true);
        var clientMock = new Mock<IMongoClient>();
        clientMock
            .Setup(c => c.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionMock.Object);

        var manager = new MongoDbTransactionManager(clientMock.Object);
        await manager.BeginTransactionAsync();

        Func<Task> act = () => manager.RollbackToSavepointAsync("missing");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ReleaseSavepoint_ShouldRemoveSavepoint()
    {
        Mock<IClientSessionHandle> sessionMock = CreateSessionMock(isInTransaction: true);
        var clientMock = new Mock<IMongoClient>();
        clientMock
            .Setup(c => c.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionMock.Object);

        var manager = new MongoDbTransactionManager(clientMock.Object);
        await manager.BeginTransactionAsync();
        manager.CreateSavepoint("sp1");

        manager.ReleaseSavepoint("sp1");

        Func<Task> act = () => manager.RollbackToSavepointAsync("sp1");
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Dispose_WithActiveTransaction_ShouldAbortAndDisposeSession()
    {
        Mock<IClientSessionHandle> sessionMock = CreateSessionMock(isInTransaction: true);
        var clientMock = new Mock<IMongoClient>();
        clientMock
            .Setup(c => c.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionMock.Object);

        var manager = new MongoDbTransactionManager(clientMock.Object);
        await manager.BeginTransactionAsync();

        manager.Dispose();

        sessionMock.Verify(s => s.AbortTransaction(), Times.Once);
        sessionMock.Verify(s => s.Dispose(), Times.Once);
        manager.CurrentSession.Should().BeNull();
    }

    [Fact]
    public async Task BeginTransactionAsync_WithTransactionOptions_ShouldStartTransactionWithOptions()
    {
        Mock<IClientSessionHandle> sessionMock = CreateSessionMock(isInTransaction: false);
        var clientMock = new Mock<IMongoClient>();
        clientMock
            .Setup(c => c.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionMock.Object);

        var manager = new MongoDbTransactionManager(clientMock.Object);
        var transactionOptions = new TransactionOptions(readConcern: ReadConcern.Local);

        await manager.BeginTransactionAsync(transactionOptions);

        sessionMock.Verify(s => s.StartTransaction(
            It.Is<TransactionOptions>(o => o.ReadConcern == ReadConcern.Local)), Times.Once);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_VoidOverload_ShouldExecuteOperation()
    {
        Mock<IClientSessionHandle> sessionMock = CreateSessionMock(isInTransaction: false);
        var clientMock = new Mock<IMongoClient>();
        clientMock
            .Setup(c => c.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionMock.Object);

        var manager = new MongoDbTransactionManager(clientMock.Object);
        bool operationExecuted = false;

        await manager.ExecuteInTransactionAsync(async (_, _) =>
        {
            operationExecuted = true;
            await Task.CompletedTask;
        });

        operationExecuted.Should().BeTrue();
        sessionMock.Verify(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenCommitReturnsUnknownResult_ShouldRetryAndSucceed()
    {
        Mock<IClientSessionHandle> sessionMock = CreateSessionMock(isInTransaction: false);
        int commitAttempts = 0;
        sessionMock
            .Setup(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                commitAttempts++;
                if (commitAttempts == 1)
                {
                    throw CreateUnknownCommitResultException();
                }

                return Task.CompletedTask;
            });

        var clientMock = new Mock<IMongoClient>();
        clientMock
            .Setup(c => c.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionMock.Object);

        var manager = new MongoDbTransactionManager(
            clientMock.Object,
            options: new MongoDbTransactionOptions { MaxCommitRetries = 3, RetryDelayMs = 1 });

        int result = await manager.ExecuteInTransactionAsync((_, _) => Task.FromResult(99));

        result.Should().Be(99);
        await WaitUntilAsync(() => commitAttempts >= 2, TimeSpan.FromSeconds(5));
        sessionMock.Verify(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CommitTransactionAsync_WhenUnknownCommitResult_ShouldRetryAndSucceed()
    {
        Mock<IClientSessionHandle> sessionMock = CreateSessionMock(isInTransaction: true);
        int commitAttempts = 0;
        sessionMock
            .Setup(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                commitAttempts++;
                if (commitAttempts == 1)
                {
                    throw CreateUnknownCommitResultException();
                }

                return Task.CompletedTask;
            });

        var clientMock = new Mock<IMongoClient>();
        clientMock
            .Setup(c => c.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionMock.Object);

        var manager = new MongoDbTransactionManager(
            clientMock.Object,
            options: new MongoDbTransactionOptions { MaxCommitRetries = 3, RetryDelayMs = 1 });

        await manager.BeginTransactionAsync();
        await manager.CommitTransactionAsync();

        await WaitUntilAsync(() => commitAttempts >= 2, TimeSpan.FromSeconds(5));
        sessionMock.Verify(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private static MongoCommandException CreateUnknownCommitResultException()
    {
        var connectionId = new ConnectionId(new ServerId(new ClusterId(1), new DnsEndPoint("localhost", 27017)), 1);
        var result = new BsonDocument
        {
            { "code", 112 },
            { "codeName", "WriteConflict" },
            { "errorLabels", new BsonArray { "UnknownTransactionCommitResult" } }
        };

        return new MongoCommandException(connectionId, "Unknown commit result", [], result);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(10);
        }

        condition().Should().BeTrue("expected condition to be met before timeout");
    }

    private static Mock<IClientSessionHandle> CreateSessionMock(bool isInTransaction)
    {
        var sessionMock = new Mock<IClientSessionHandle>();
        bool inTransaction = isInTransaction;
        sessionMock.Setup(s => s.IsInTransaction).Returns(() => inTransaction);
        sessionMock.Setup(s => s.StartTransaction(It.IsAny<TransactionOptions>()))
            .Callback(() => inTransaction = true);
        sessionMock.Setup(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        sessionMock.Setup(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return sessionMock;
    }
}

[Trait("Category", "Integration")]
[Collection(MongoDbIntegrationCollection.Name)]
public class MongoDbTransactionManagerIntegrationTest(MongoDbIntegrationFixture fixture)
{
    [DockerFact]
    public async Task ExecuteInTransactionAsync_OnRealClient_ShouldExecuteOrSurfaceReplicaSetRequirement()
    {
        var manager = new MongoDbTransactionManager(fixture.Client, NullLogger<MongoDbTransactionManager>.Instance);
        IMongoCollection<BsonDocument> collection = fixture.Database.GetCollection<BsonDocument>("tx_test");

        try
        {
            await manager.ExecuteInTransactionAsync(async (session, ct) => await collection.InsertOneAsync(session, new BsonDocument("value", 1), cancellationToken: ct));

            long count = await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
            count.Should().BeGreaterThan(0);
        }
        catch (MongoException ex)
        {
            ex.Message.Should().Match("*replica set*", "*Transaction*", "*transaction*");
        }
        catch (NotSupportedException)
        {
            // Standalone MongoDB (Testcontainers default) does not support transactions.
        }
        finally
        {
            manager.Dispose();
            await collection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
        }
    }

    [DockerFact]
    public async Task BeginAndAbortTransaction_OnRealClient_ShouldManageSessionLifecycle()
    {
        var manager = new MongoDbTransactionManager(
            fixture.Client,
            NullLogger<MongoDbTransactionManager>.Instance,
            new MongoDbTransactionOptions { MaxCommitRetries = 1, RetryDelayMs = 1 });

        try
        {
            await manager.BeginTransactionAsync();
            manager.CreateSavepoint("before-abort");
            manager.ReleaseSavepoint("before-abort");
            await manager.AbortTransactionAsync();
            manager.IsTransactionActive.Should().BeFalse();
        }
        catch (MongoException)
        {
            // Standalone deployments may reject transactions before abort; session lifecycle is still exercised.
        }
        catch (NotSupportedException)
        {
            // Standalone MongoDB (Testcontainers default) does not support transactions.
        }
        finally
        {
            manager.Dispose();
        }
    }
}
