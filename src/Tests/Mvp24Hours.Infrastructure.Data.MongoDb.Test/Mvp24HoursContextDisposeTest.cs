using System.Diagnostics;
using System.Reflection;
using Moq;
using MongoDB.Driver;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test;

[Trait("Category", "Unit")]
public class Mvp24HoursContextDisposeTest
{
    private const string ConnectionString = "mongodb://127.0.0.1:27017";

    private static Mvp24HoursContext CreateContext(string databaseName)
        => new(databaseName, ConnectionString);

    private static void SetSession(Mvp24HoursContext context, IClientSessionHandle session)
    {
        PropertyInfo? property = typeof(Mvp24HoursContext).GetProperty(nameof(Mvp24HoursContext.Session));
        property.Should().NotBeNull();
        property!.SetValue(context, session);
    }

    private static Mock<IClientSessionHandle> CreateSessionMock(bool isInTransaction)
    {
        var sessionMock = new Mock<IClientSessionHandle>();
        sessionMock.Setup(s => s.IsInTransaction).Returns(isInTransaction);
        return sessionMock;
    }

    [Fact]
    public void Dispose_WithoutSession_DoesNotThrow()
    {
        var context = CreateContext("dispose_no_session_db");

        Action act = () => context.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_WithOpenTransaction_CommitsAndDisposesSession()
    {
        var context = CreateContext("dispose_commit_db");
        Mock<IClientSessionHandle> sessionMock = CreateSessionMock(isInTransaction: true);
        SetSession(context, sessionMock.Object);

        context.Dispose();

        sessionMock.Verify(s => s.CommitTransaction(It.IsAny<CancellationToken>()), Times.Once);
        sessionMock.Verify(s => s.AbortTransaction(It.IsAny<CancellationToken>()), Times.Never);
        sessionMock.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_WhenCommitThrows_AbortsAndDisposesSession()
    {
        var context = CreateContext("dispose_commit_throws_db");
        Mock<IClientSessionHandle> sessionMock = CreateSessionMock(isInTransaction: true);
        sessionMock.Setup(s => s.CommitTransaction(It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("commit failed"));
        SetSession(context, sessionMock.Object);

        Action act = () => context.Dispose();

        act.Should().NotThrow();
        sessionMock.Verify(s => s.CommitTransaction(It.IsAny<CancellationToken>()), Times.Once);
        sessionMock.Verify(s => s.AbortTransaction(It.IsAny<CancellationToken>()), Times.Once);
        sessionMock.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_WithOpenTransaction_CommitsAsync()
    {
        var context = CreateContext("dispose_async_commit_db");
        Mock<IClientSessionHandle> sessionMock = CreateSessionMock(isInTransaction: true);
        sessionMock.Setup(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        SetSession(context, sessionMock.Object);

        await context.DisposeAsync();

        sessionMock.Verify(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        sessionMock.Verify(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        sessionMock.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_WhenCommitThrows_AbortsAsyncAndDisposesSession()
    {
        var context = CreateContext("dispose_async_commit_throws_db");
        Mock<IClientSessionHandle> sessionMock = CreateSessionMock(isInTransaction: true);
        sessionMock.Setup(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("commit failed"));
        sessionMock.Setup(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        SetSession(context, sessionMock.Object);

        Func<Task> act = () => context.DisposeAsync().AsTask();

        await act.Should().NotThrowAsync();
        sessionMock.Verify(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        sessionMock.Verify(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        sessionMock.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_WithOpenTransaction_CompletesUnder1Second()
    {
        var context = CreateContext("dispose_fast_db");
        Mock<IClientSessionHandle> sessionMock = CreateSessionMock(isInTransaction: true);
        SetSession(context, sessionMock.Object);

        var stopwatch = Stopwatch.StartNew();
        context.Dispose();
        stopwatch.Stop();

        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1));
    }
}
