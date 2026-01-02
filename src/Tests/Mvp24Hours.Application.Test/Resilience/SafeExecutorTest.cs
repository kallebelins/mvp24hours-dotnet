//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mvp24Hours.Application.Contract.Resilience;
using Mvp24Hours.Application.Logic.Resilience;
using Mvp24Hours.Core.Exceptions;
using Xunit;

namespace Mvp24Hours.Application.Test.Resilience;

/// <summary>
/// Unit tests for SafeExecutor static class functionality.
/// </summary>
public class SafeExecutorTest
{
    private readonly IExceptionToResultMapper _mapper;
    private readonly ILogger _logger;

    public SafeExecutorTest()
    {
        _mapper = new ExceptionToResultMapper();
        _logger = NullLogger.Instance;
    }

    #region [ Synchronous Execute Tests ]

    [Fact]
    public void Execute_SuccessfulOperation_ShouldReturnSuccessResult()
    {
        // Act
        var result = SafeExecutor.Execute(() => "Success", _mapper, _logger);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.Data.Should().Be("Success");
        result.StatusCode.Should().Be(ResultStatusCode.Success);
    }

    [Fact]
    public void Execute_OperationThrowsNotFoundException_ShouldReturnNotFoundResult()
    {
        // Act
        var result = SafeExecutor.Execute<string>(
            () => throw new NotFoundException("Resource not found"),
            _mapper,
            _logger);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.StatusCode.Should().Be(ResultStatusCode.NotFound);
    }

    [Fact]
    public void Execute_OperationThrowsValidationException_ShouldReturnValidationFailedResult()
    {
        // Act
        var result = SafeExecutor.Execute<string>(
            () => throw new ValidationException("Validation failed"),
            _mapper,
            _logger);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.StatusCode.Should().Be(ResultStatusCode.ValidationFailed);
    }

    [Fact]
    public void Execute_OperationThrowsUnknownException_ShouldReturnInternalErrorResult()
    {
        // Act
        var result = SafeExecutor.Execute<string>(
            () => throw new Exception("Unknown error"),
            _mapper,
            _logger);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.StatusCode.Should().Be(ResultStatusCode.InternalError);
    }

    [Fact]
    public void Execute_WithNullResult_ShouldReturnSuccessWithNullData()
    {
        // Act
        var result = SafeExecutor.Execute<string?>(() => null, _mapper, _logger);

        // Assert
        result.HasErrors.Should().BeFalse();
        result.Data.Should().BeNull();
    }

    [Fact]
    public void Execute_WithComplexType_ShouldReturnCorrectData()
    {
        // Arrange
        var expected = new TestEntity { Id = 1, Name = "Test" };

        // Act
        var result = SafeExecutor.Execute(() => expected, _mapper, _logger);

        // Assert
        result.HasErrors.Should().BeFalse();
        result.Data.Should().Be(expected);
        result.Data?.Id.Should().Be(1);
        result.Data?.Name.Should().Be("Test");
    }

    [Fact]
    public void Execute_VoidOperation_ShouldReturnTrueOnSuccess()
    {
        // Arrange
        var executed = false;

        // Act
        var result = SafeExecutor.Execute(() => { executed = true; }, _mapper, _logger);

        // Assert
        result.HasErrors.Should().BeFalse();
        result.Data.Should().BeTrue();
        executed.Should().BeTrue();
    }

    [Fact]
    public void Execute_VoidOperationThrows_ShouldReturnFailure()
    {
        // Act
        var result = SafeExecutor.Execute(
            () => throw new NotFoundException("Not found"),
            _mapper,
            _logger);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.StatusCode.Should().Be(ResultStatusCode.NotFound);
    }

    [Fact]
    public void Execute_WithCustomErrorMessage_ShouldIncludeCustomMessage()
    {
        // Arrange
        var customMessage = "Custom error message";

        // Act
        var result = SafeExecutor.Execute<string>(
            () => throw new NotFoundException("Original message"),
            _mapper,
            customMessage,
            _logger);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message == customMessage);
    }

    [Fact]
    public void Execute_WithResultMapping_ShouldMapCorrectly()
    {
        // Act
        var result = SafeExecutor.Execute(
            () => 42,
            x => x.ToString(),
            _mapper,
            _logger);

        // Assert
        result.HasErrors.Should().BeFalse();
        result.Data.Should().Be("42");
    }

    #endregion

    #region [ Asynchronous Execute Tests ]

    [Fact]
    public async Task ExecuteAsync_SuccessfulOperation_ShouldReturnSuccessResult()
    {
        // Act
        var result = await SafeExecutor.ExecuteAsync(async () =>
        {
            await Task.Delay(1);
            return "Success";
        }, _mapper, _logger);

        // Assert
        result.HasErrors.Should().BeFalse();
        result.Data.Should().Be("Success");
    }

    [Fact]
    public async Task ExecuteAsync_OperationThrowsNotFoundException_ShouldReturnNotFoundResult()
    {
        // Act
        var result = await SafeExecutor.ExecuteAsync<string>(async () =>
        {
            await Task.Delay(1);
            throw new NotFoundException("Resource not found");
        }, _mapper, _logger);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.StatusCode.Should().Be(ResultStatusCode.NotFound);
    }

    [Fact]
    public async Task ExecuteAsync_OperationThrowsTimeoutException_ShouldReturnTimeoutResult()
    {
        // Act
        var result = await SafeExecutor.ExecuteAsync<string>(async () =>
        {
            await Task.Delay(1);
            throw new TimeoutException("Operation timed out");
        }, _mapper, _logger);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.StatusCode.Should().Be(ResultStatusCode.Timeout);
    }

    [Fact]
    public async Task ExecuteAsync_WithComplexType_ShouldReturnCorrectData()
    {
        // Arrange
        var expected = new TestEntity { Id = 42, Name = "AsyncTest" };

        // Act
        var result = await SafeExecutor.ExecuteAsync(async () =>
        {
            await Task.Delay(1);
            return expected;
        }, _mapper, _logger);

        // Assert
        result.HasErrors.Should().BeFalse();
        result.Data.Should().Be(expected);
    }

    [Fact]
    public async Task ExecuteAsync_VoidOperation_ShouldReturnTrueOnSuccess()
    {
        // Arrange
        var executed = false;

        // Act
        var result = await SafeExecutor.ExecuteAsync(async () =>
        {
            await Task.Delay(1);
            executed = true;
        }, _mapper, _logger);

        // Assert
        result.HasErrors.Should().BeFalse();
        result.Data.Should().BeTrue();
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomErrorMessage_ShouldIncludeCustomMessage()
    {
        // Arrange
        var customMessage = "Custom async error message";

        // Act
        var result = await SafeExecutor.ExecuteAsync<string>(
            async () =>
            {
                await Task.Delay(1);
                throw new NotFoundException("Original message");
            },
            _mapper,
            customMessage,
            _logger);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message == customMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WithResultMapping_ShouldMapCorrectly()
    {
        // Act
        var result = await SafeExecutor.ExecuteAsync(
            async () =>
            {
                await Task.Delay(1);
                return 42;
            },
            x => x * 2,
            _mapper,
            _logger);

        // Assert
        result.HasErrors.Should().BeFalse();
        result.Data.Should().Be(84);
    }

    [Fact]
    public async Task ExecuteAsync_WithAsyncResultMapping_ShouldMapCorrectly()
    {
        // Act
        var result = await SafeExecutor.ExecuteAsync(
            async () =>
            {
                await Task.Delay(1);
                return 42;
            },
            async x =>
            {
                await Task.Delay(1);
                return x.ToString();
            },
            _mapper,
            _logger);

        // Assert
        result.HasErrors.Should().BeFalse();
        result.Data.Should().Be("42");
    }

    #endregion

    #region [ ExecuteOrNotFound Tests ]

    [Fact]
    public void ExecuteOrNotFound_WithNullResult_ShouldReturnNotFound()
    {
        // Act
        var result = SafeExecutor.ExecuteOrNotFound<TestEntity>(
            () => null,
            _mapper,
            "Entity not found",
            _logger);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.StatusCode.Should().Be(ResultStatusCode.NotFound);
    }

    [Fact]
    public void ExecuteOrNotFound_WithResult_ShouldReturnSuccess()
    {
        // Arrange
        var expected = new TestEntity { Id = 1, Name = "Test" };

        // Act
        var result = SafeExecutor.ExecuteOrNotFound(
            () => expected,
            _mapper,
            "Entity not found",
            _logger);

        // Assert
        result.HasErrors.Should().BeFalse();
        result.Data.Should().Be(expected);
    }

    [Fact]
    public async Task ExecuteOrNotFoundAsync_WithNullResult_ShouldReturnNotFound()
    {
        // Act
        var result = await SafeExecutor.ExecuteOrNotFoundAsync<TestEntity>(
            async () =>
            {
                await Task.Delay(1);
                return null;
            },
            _mapper,
            "Entity not found",
            _logger);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.StatusCode.Should().Be(ResultStatusCode.NotFound);
    }

    [Fact]
    public async Task ExecuteOrNotFoundAsync_WithResult_ShouldReturnSuccess()
    {
        // Arrange
        var expected = new TestEntity { Id = 1, Name = "Test" };

        // Act
        var result = await SafeExecutor.ExecuteOrNotFoundAsync(
            async () =>
            {
                await Task.Delay(1);
                return expected;
            },
            _mapper,
            "Entity not found",
            _logger);

        // Assert
        result.HasErrors.Should().BeFalse();
        result.Data.Should().Be(expected);
    }

    #endregion

    #region [ Logging Tests ]

    [Fact]
    public void Execute_WithLoggedException_ShouldLog()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        // Act
        var result = SafeExecutor.Execute<string>(
            () => throw new Exception("Critical error"),
            _mapper,
            mockLogger.Object);

        // Assert
        result.HasErrors.Should().BeTrue();
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeastOnce);
    }

    [Fact]
    public void Execute_WithNonLoggedException_ShouldNotLogAsError()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        // Act - NotFoundException typically doesn't require logging as error
        var result = SafeExecutor.Execute<string>(
            () => throw new NotFoundException("Not found"),
            _mapper,
            mockLogger.Object);

        // Assert
        result.HasErrors.Should().BeTrue();
        // Verify that no Error level logging occurred (NotFoundException shouldn't log as error)
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Never);
    }

    #endregion

    #region [ Argument Validation Tests ]

    [Fact]
    public void Execute_WithNullOperation_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => SafeExecutor.Execute<string>(null!, _mapper, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("operation");
    }

    [Fact]
    public void Execute_WithNullMapper_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => SafeExecutor.Execute(() => "test", null!, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("mapper");
    }

    [Fact]
    public async Task ExecuteAsync_WithNullOperation_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = async () => await SafeExecutor.ExecuteAsync<string>(null!, _mapper, _logger);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("operation");
    }

    [Fact]
    public async Task ExecuteAsync_WithNullMapper_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = async () => await SafeExecutor.ExecuteAsync(async () => { await Task.Delay(1); return "test"; }, null!, _logger);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("mapper");
    }

    #endregion

    #region [ Edge Cases Tests ]

    [Fact]
    public void Execute_ActionThrowsArgumentNullException_ShouldReturnValidationFailedResult()
    {
        // Act
        var result = SafeExecutor.Execute<string>(
            () => throw new ArgumentNullException("param"),
            _mapper,
            _logger);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.StatusCode.Should().Be(ResultStatusCode.ValidationFailed);
    }

    [Fact]
    public void Execute_ActionThrowsUnauthorizedException_ShouldReturnUnauthorizedResult()
    {
        // Act
        var result = SafeExecutor.Execute<string>(
            () => throw new UnauthorizedException("Not authenticated"),
            _mapper,
            _logger);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.StatusCode.Should().Be(ResultStatusCode.Unauthorized);
    }

    [Fact]
    public void Execute_ActionThrowsForbiddenException_ShouldReturnForbiddenResult()
    {
        // Act
        var result = SafeExecutor.Execute<string>(
            () => throw new ForbiddenException("Access denied"),
            _mapper,
            _logger);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.StatusCode.Should().Be(ResultStatusCode.Forbidden);
    }

    [Fact]
    public void Execute_ActionThrowsConflictException_ShouldReturnConflictResult()
    {
        // Act
        var result = SafeExecutor.Execute<string>(
            () => throw new ConflictException("Resource conflict"),
            _mapper,
            _logger);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.StatusCode.Should().Be(ResultStatusCode.Conflict);
    }

    [Fact]
    public void Execute_ActionThrowsDomainException_ShouldReturnDomainRuleViolationResult()
    {
        // Act
        var result = SafeExecutor.Execute<string>(
            () => throw new DomainException("Domain rule violated"),
            _mapper,
            _logger);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.StatusCode.Should().Be(ResultStatusCode.DomainRuleViolation);
    }

    [Fact]
    public async Task ExecuteAsync_MultipleExceptionsInSequence_ShouldHandleEachIndependently()
    {
        // Arrange
        var count = 0;

        // Act & Assert
        var result1 = await SafeExecutor.ExecuteAsync<string>(async () =>
        {
            await Task.Delay(1);
            count++;
            throw new NotFoundException("Not found 1");
        }, _mapper, _logger);
        result1.StatusCode.Should().Be(ResultStatusCode.NotFound);

        var result2 = await SafeExecutor.ExecuteAsync<string>(async () =>
        {
            await Task.Delay(1);
            count++;
            throw new ValidationException("Validation error");
        }, _mapper, _logger);
        result2.StatusCode.Should().Be(ResultStatusCode.ValidationFailed);

        var result3 = await SafeExecutor.ExecuteAsync(async () =>
        {
            await Task.Delay(1);
            count++;
            return "Success";
        }, _mapper, _logger);
        result3.HasErrors.Should().BeFalse();

        count.Should().Be(3);
    }

    #endregion

    #region [ Test Support Classes ]

    private class TestEntity
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    #endregion
}
