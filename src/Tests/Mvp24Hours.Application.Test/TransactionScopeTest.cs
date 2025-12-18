//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Application.Contract.Transaction;
using Mvp24Hours.Application.Extensions;
using Mvp24Hours.Application.Logic.Transaction;
using Mvp24Hours.Application.Test.Support;
using Mvp24Hours.Core.Contract.Data;

namespace Mvp24Hours.Application.Test;

/// <summary>
/// Unit tests for TransactionScope and related transaction functionality.
/// </summary>
public class TransactionScopeTest
{
    #region [ TransactionScope Basic Tests ]

    [Fact]
    public async Task TransactionScope_InitialStatus_ShouldBeNotStarted()
    {
        // Arrange
        var unitOfWork = new MockUnitOfWorkAsync();
        var scope = new TransactionScope(unitOfWork);

        // Assert
        scope.Status.Should().Be(TransactionStatus.NotStarted);
        scope.IsActive.Should().BeFalse();
        scope.TransactionId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task BeginAsync_ShouldSetStatusToActive()
    {
        // Arrange
        var unitOfWork = new MockUnitOfWorkAsync();
        await using var scope = new TransactionScope(unitOfWork);

        // Act
        await scope.BeginAsync();

        // Assert
        scope.Status.Should().Be(TransactionStatus.Active);
        scope.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task BeginAsync_CalledTwice_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var unitOfWork = new MockUnitOfWorkAsync();
        await using var scope = new TransactionScope(unitOfWork);
        await scope.BeginAsync();

        // Act & Assert
        await scope.Invoking(s => s.BeginAsync())
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CommitAsync_ShouldCallSaveChangesAndSetCommittedStatus()
    {
        // Arrange
        var unitOfWork = new MockUnitOfWorkAsync();
        unitOfWork.SetSaveChangesResult(5);
        await using var scope = new TransactionScope(unitOfWork);
        await scope.BeginAsync();

        // Act
        var result = await scope.CommitAsync();

        // Assert
        result.Should().Be(5);
        scope.Status.Should().Be(TransactionStatus.Committed);
        unitOfWork.SaveChangesCalled.Should().BeTrue();
    }

    [Fact]
    public async Task CommitAsync_WhenNotActive_ShouldThrowTransactionException()
    {
        // Arrange
        var unitOfWork = new MockUnitOfWorkAsync();
        await using var scope = new TransactionScope(unitOfWork);

        // Act & Assert
        await scope.Invoking(s => s.CommitAsync())
            .Should().ThrowAsync<TransactionException>();
    }

    [Fact]
    public async Task CommitAsync_WhenSaveChangesFails_ShouldThrowTransactionException()
    {
        // Arrange
        var unitOfWork = new MockUnitOfWorkAsync();
        unitOfWork.SetThrowOnSaveChanges(true);
        await using var scope = new TransactionScope(unitOfWork);
        await scope.BeginAsync();

        // Act & Assert
        await scope.Invoking(s => s.CommitAsync())
            .Should().ThrowAsync<TransactionException>()
            .Where(ex => ex.ErrorCode == "COMMIT_FAILED");
    }

    [Fact]
    public async Task RollbackAsync_ShouldCallRollbackAndSetRolledBackStatus()
    {
        // Arrange
        var unitOfWork = new MockUnitOfWorkAsync();
        await using var scope = new TransactionScope(unitOfWork);
        await scope.BeginAsync();

        // Act
        await scope.RollbackAsync();

        // Assert
        scope.Status.Should().Be(TransactionStatus.RolledBack);
        unitOfWork.RollbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task RollbackAsync_WhenNotStarted_ShouldNotThrow()
    {
        // Arrange
        var unitOfWork = new MockUnitOfWorkAsync();
        await using var scope = new TransactionScope(unitOfWork);

        // Act & Assert - should not throw
        await scope.RollbackAsync();
        scope.Status.Should().Be(TransactionStatus.NotStarted);
    }

    [Fact]
    public async Task RollbackAsync_WhenAlreadyCommitted_ShouldNotCallRollback()
    {
        // Arrange
        var unitOfWork = new MockUnitOfWorkAsync();
        await using var scope = new TransactionScope(unitOfWork);
        await scope.BeginAsync();
        await scope.CommitAsync();

        // Act
        await scope.RollbackAsync();

        // Assert
        unitOfWork.RollbackCalled.Should().BeFalse();
    }

    #endregion

    #region [ Execute Pattern Tests ]

    [Fact]
    public async Task ExecuteAsync_ActionSuccess_ShouldCommit()
    {
        // Arrange
        var unitOfWork = new MockUnitOfWorkAsync();
        unitOfWork.SetSaveChangesResult(3);
        await using var scope = new TransactionScope(unitOfWork);
        var executed = false;

        // Act
        var result = await scope.ExecuteAsync(async () =>
        {
            executed = true;
            await Task.Delay(1);
        });

        // Assert
        executed.Should().BeTrue();
        result.Should().Be(3);
        scope.Status.Should().Be(TransactionStatus.Committed);
    }

    [Fact]
    public async Task ExecuteAsync_ActionFails_ShouldRollback()
    {
        // Arrange
        var unitOfWork = new MockUnitOfWorkAsync();
        await using var scope = new TransactionScope(unitOfWork);

        // Act & Assert
        await scope.Invoking(async s =>
        {
            await s.ExecuteAsync(async () =>
            {
                await Task.Delay(1);
                throw new InvalidOperationException("Business error");
            });
        }).Should().ThrowAsync<InvalidOperationException>();

        scope.Status.Should().Be(TransactionStatus.RolledBack);
        unitOfWork.RollbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithResult_ShouldReturnResultAndAffectedRows()
    {
        // Arrange
        var unitOfWork = new MockUnitOfWorkAsync();
        unitOfWork.SetSaveChangesResult(2);
        await using var scope = new TransactionScope(unitOfWork);

        // Act
        var (result, affectedRows) = await scope.ExecuteAsync(async () =>
        {
            await Task.Delay(1);
            return "Success";
        });

        // Assert
        result.Should().Be("Success");
        affectedRows.Should().Be(2);
        scope.Status.Should().Be(TransactionStatus.Committed);
    }

    #endregion

    #region [ Savepoint Tests ]

    [Fact]
    public async Task CreateSavepointAsync_ShouldIncreaseNestingLevel()
    {
        // Arrange
        var unitOfWork = new MockUnitOfWorkAsync();
        await using var scope = new TransactionScope(unitOfWork);
        await scope.BeginAsync();

        // Act
        await scope.CreateSavepointAsync("sp1");
        await scope.CreateSavepointAsync("sp2");

        // Assert
        scope.NestingLevel.Should().Be(2);
    }

    [Fact]
    public async Task CreateSavepointAsync_WhenNotActive_ShouldThrow()
    {
        // Arrange
        var unitOfWork = new MockUnitOfWorkAsync();
        await using var scope = new TransactionScope(unitOfWork);

        // Act & Assert
        await scope.Invoking(s => s.CreateSavepointAsync("sp1"))
            .Should().ThrowAsync<TransactionException>();
    }

    [Fact]
    public async Task CreateSavepointAsync_WithNullName_ShouldThrow()
    {
        // Arrange
        var unitOfWork = new MockUnitOfWorkAsync();
        await using var scope = new TransactionScope(unitOfWork);
        await scope.BeginAsync();

        // Act & Assert
        await scope.Invoking(s => s.CreateSavepointAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region [ Disposal Tests ]

    [Fact]
    public async Task DisposeAsync_WhenActive_ShouldAutoRollback()
    {
        // Arrange
        var unitOfWork = new MockUnitOfWorkAsync();
        
        // Act
        await using (var scope = new TransactionScope(unitOfWork))
        {
            await scope.BeginAsync();
            // Not committing - should auto-rollback on dispose
        }

        // Assert
        unitOfWork.RollbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAsync_WhenCommitted_ShouldNotRollback()
    {
        // Arrange
        var unitOfWork = new MockUnitOfWorkAsync();

        // Act
        await using (var scope = new TransactionScope(unitOfWork))
        {
            await scope.BeginAsync();
            await scope.CommitAsync();
        }

        // Assert
        unitOfWork.RollbackCalled.Should().BeFalse();
    }

    [Fact]
    public void Dispose_WhenActive_ShouldAutoRollback()
    {
        // Arrange
        var unitOfWork = new MockUnitOfWorkAsync();

        // Act
        using (var scope = new TransactionScope(unitOfWork))
        {
            scope.BeginAsync().GetAwaiter().GetResult();
            // Not committing - should auto-rollback on dispose
        }

        // Assert
        unitOfWork.RollbackCalled.Should().BeTrue();
    }

    #endregion

    #region [ TransactionScopeSync Tests ]

    [Fact]
    public void TransactionScopeSync_BeginCommit_ShouldWork()
    {
        // Arrange
        var unitOfWork = new MockUnitOfWork();
        unitOfWork.SetSaveChangesResult(4);
        using var scope = new TransactionScopeSync(unitOfWork);

        // Act
        scope.Begin();
        var result = scope.Commit();

        // Assert
        result.Should().Be(4);
        scope.Status.Should().Be(TransactionStatus.Committed);
        unitOfWork.SaveChangesCalled.Should().BeTrue();
    }

    [Fact]
    public void TransactionScopeSync_Execute_ShouldCommitOnSuccess()
    {
        // Arrange
        var unitOfWork = new MockUnitOfWork();
        unitOfWork.SetSaveChangesResult(2);
        using var scope = new TransactionScopeSync(unitOfWork);
        var executed = false;

        // Act
        var result = scope.Execute(() => { executed = true; });

        // Assert
        executed.Should().BeTrue();
        result.Should().Be(2);
        scope.Status.Should().Be(TransactionStatus.Committed);
    }

    [Fact]
    public void TransactionScopeSync_Execute_ShouldRollbackOnFailure()
    {
        // Arrange
        var unitOfWork = new MockUnitOfWork();
        using var scope = new TransactionScopeSync(unitOfWork);

        // Act & Assert
        scope.Invoking(s => s.Execute(() => throw new InvalidOperationException("Error")))
            .Should().Throw<InvalidOperationException>();

        scope.Status.Should().Be(TransactionStatus.RolledBack);
        unitOfWork.RollbackCalled.Should().BeTrue();
    }

    #endregion

    #region [ AmbientTransactionContext Tests ]

    [Fact]
    public async Task AmbientTransactionContext_AfterBegin_ShouldHaveCurrent()
    {
        // Arrange
        var unitOfWork = new MockUnitOfWorkAsync();
        await using var scope = new TransactionScope(unitOfWork);

        // Act
        await scope.BeginAsync();

        // Assert
        AmbientTransactionContext.HasCurrent.Should().BeTrue();
        AmbientTransactionContext.CurrentTransactionId.Should().Be(scope.TransactionId);
    }

    [Fact]
    public async Task AmbientTransactionContext_AfterCommit_ShouldNotHaveCurrent()
    {
        // Arrange
        var unitOfWork = new MockUnitOfWorkAsync();
        await using var scope = new TransactionScope(unitOfWork);
        await scope.BeginAsync();

        // Act
        await scope.CommitAsync();

        // Assert
        // HasCurrent should be false because IsActive is false after commit
        AmbientTransactionContext.HasCurrent.Should().BeFalse();
        // Current may still reference the scope, but it's no longer active
        AmbientTransactionContext.CurrentStatus.Should().Be(TransactionStatus.Committed);
    }

    [Fact]
    public async Task AmbientTransactionContext_AfterRollback_ShouldNotHaveCurrent()
    {
        // Arrange
        var unitOfWork = new MockUnitOfWorkAsync();
        await using var scope = new TransactionScope(unitOfWork);
        await scope.BeginAsync();

        // Act
        await scope.RollbackAsync();

        // Assert
        AmbientTransactionContext.HasCurrent.Should().BeFalse();
    }

    #endregion

    #region [ TransactionScopeFactory Tests ]

    [Fact]
    public void TransactionScopeFactory_Create_ShouldReturnNewScope()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IUnitOfWorkAsync, MockUnitOfWorkAsync>();
        services.AddTransactionScope();
        var provider = services.BuildServiceProvider();

        // Act
        var factory = provider.GetRequiredService<ITransactionScopeFactory>();
        var scope = factory.Create();

        // Assert
        scope.Should().NotBeNull();
        scope.Status.Should().Be(TransactionStatus.NotStarted);
    }

    [Fact]
    public void TransactionScopeFactory_CreateSync_ShouldReturnNewScope()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IUnitOfWork, MockUnitOfWork>();
        services.AddTransactionScope();
        var provider = services.BuildServiceProvider();

        // Act
        var factory = provider.GetRequiredService<ITransactionScopeFactory>();
        var scope = factory.CreateSync();

        // Assert
        scope.Should().NotBeNull();
        scope.Status.Should().Be(TransactionStatus.NotStarted);
    }

    #endregion

    #region [ DI Registration Tests ]

    [Fact]
    public void AddTransactionScope_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IUnitOfWorkAsync, MockUnitOfWorkAsync>();
        services.AddSingleton<IUnitOfWork, MockUnitOfWork>();

        // Act
        services.AddTransactionScope();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<ITransactionScopeFactory>().Should().NotBeNull();
        provider.GetService<ITransactionScope>().Should().NotBeNull();
        provider.GetService<ITransactionScopeSync>().Should().NotBeNull();
    }

    [Fact]
    public void AddTransactionScope_WithOptions_ShouldRegisterOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IUnitOfWorkAsync, MockUnitOfWorkAsync>();

        // Act
        services.AddTransactionScope(options =>
        {
            options.DefaultTimeoutSeconds = 60;
            options.EnableRetryOnTransientFailure = true;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<TransactionScopeOptions>();
        options.Should().NotBeNull();
        options!.DefaultTimeoutSeconds.Should().Be(60);
        options.EnableRetryOnTransientFailure.Should().BeTrue();
    }

    #endregion

    #region [ TransactionException Tests ]

    [Fact]
    public void TransactionException_CommitFailed_ShouldHaveCorrectProperties()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var innerEx = new InvalidOperationException("DB Error");

        // Act
        var ex = TransactionException.CommitFailed(transactionId, innerEx);

        // Assert
        ex.TransactionId.Should().Be(transactionId);
        ex.ErrorCode.Should().Be("COMMIT_FAILED");
        ex.InnerException.Should().Be(innerEx);
        ex.TransactionStatus.Should().Be(TransactionStatus.Error);
    }

    [Fact]
    public void TransactionException_RollbackFailed_ShouldHaveCorrectProperties()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var innerEx = new InvalidOperationException("Rollback Error");

        // Act
        var ex = TransactionException.RollbackFailed(transactionId, innerEx);

        // Assert
        ex.TransactionId.Should().Be(transactionId);
        ex.ErrorCode.Should().Be("ROLLBACK_FAILED");
        ex.InnerException.Should().Be(innerEx);
    }

    [Fact]
    public void TransactionException_InvalidState_ShouldHaveCorrectProperties()
    {
        // Arrange
        var transactionId = Guid.NewGuid();

        // Act
        var ex = TransactionException.InvalidState(transactionId, TransactionStatus.NotStarted, "commit");

        // Assert
        ex.TransactionId.Should().Be(transactionId);
        ex.ErrorCode.Should().Be("INVALID_STATE");
        ex.TransactionStatus.Should().Be(TransactionStatus.NotStarted);
    }

    #endregion
}

