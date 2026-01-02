//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentAssertions;
using Microsoft.Extensions.Options;
using Mvp24Hours.Application.Contract.Resilience;
using Mvp24Hours.Application.Logic.Resilience;
using Mvp24Hours.Core.Exceptions;
using Xunit;

namespace Mvp24Hours.Application.Test.Resilience;

/// <summary>
/// Unit tests for ExceptionToResultMapper functionality.
/// </summary>
public class ExceptionToResultMapperTest
{
    private readonly IExceptionToResultMapper _mapper;

    public ExceptionToResultMapperTest()
    {
        _mapper = new ExceptionToResultMapper();
    }

    #region [ Map Tests ]

    [Fact]
    public void Map_NotFoundException_ShouldReturnNotFoundStatusCode()
    {
        // Arrange
        var exception = new NotFoundException("Resource not found");

        // Act
        var result = _mapper.Map<string>(exception);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(ResultStatusCode.NotFound);
        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public void Map_ConflictException_ShouldReturnConflictStatusCode()
    {
        // Arrange
        var exception = new ConflictException("Resource conflict");

        // Act
        var result = _mapper.Map<string>(exception);

        // Assert
        result.StatusCode.Should().Be(ResultStatusCode.Conflict);
        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public void Map_UnauthorizedException_ShouldReturnUnauthorizedStatusCode()
    {
        // Arrange
        var exception = new UnauthorizedException("Not authenticated");

        // Act
        var result = _mapper.Map<string>(exception);

        // Assert
        result.StatusCode.Should().Be(ResultStatusCode.Unauthorized);
    }

    [Fact]
    public void Map_ForbiddenException_ShouldReturnForbiddenStatusCode()
    {
        // Arrange
        var exception = new ForbiddenException("Access denied");

        // Act
        var result = _mapper.Map<string>(exception);

        // Assert
        result.StatusCode.Should().Be(ResultStatusCode.Forbidden);
    }

    [Fact]
    public void Map_ValidationException_ShouldReturnValidationFailedStatusCode()
    {
        // Arrange
        var exception = new ValidationException("Validation failed");

        // Act
        var result = _mapper.Map<string>(exception);

        // Assert
        result.StatusCode.Should().Be(ResultStatusCode.ValidationFailed);
    }

    [Fact]
    public void Map_DomainException_ShouldReturnDomainRuleViolationStatusCode()
    {
        // Arrange
        var exception = new DomainException("Domain rule violated");

        // Act
        var result = _mapper.Map<string>(exception);

        // Assert
        result.StatusCode.Should().Be(ResultStatusCode.DomainRuleViolation);
    }

    [Fact]
    public void Map_ArgumentNullException_ShouldReturnValidationFailedStatusCode()
    {
        // Arrange
        var exception = new ArgumentNullException("param", "Parameter cannot be null");

        // Act
        var result = _mapper.Map<string>(exception);

        // Assert
        result.StatusCode.Should().Be(ResultStatusCode.ValidationFailed);
    }

    [Fact]
    public void Map_ArgumentOutOfRangeException_ShouldReturnOutOfRangeStatusCode()
    {
        // Arrange
        var exception = new ArgumentOutOfRangeException("param", "Value out of range");

        // Act
        var result = _mapper.Map<string>(exception);

        // Assert
        result.StatusCode.Should().Be(ResultStatusCode.OutOfRange);
    }

    [Fact]
    public void Map_TimeoutException_ShouldReturnTimeoutStatusCode()
    {
        // Arrange
        var exception = new TimeoutException("Operation timed out");

        // Act
        var result = _mapper.Map<string>(exception);

        // Assert
        result.StatusCode.Should().Be(ResultStatusCode.Timeout);
    }

    [Fact]
    public void Map_OperationCanceledException_ShouldReturnOperationCancelledStatusCode()
    {
        // Arrange
        var exception = new OperationCanceledException("Operation cancelled");

        // Act
        var result = _mapper.Map<string>(exception);

        // Assert
        result.StatusCode.Should().Be(ResultStatusCode.OperationCancelled);
    }

    [Fact]
    public void Map_NotSupportedException_ShouldReturnOperationNotSupportedStatusCode()
    {
        // Arrange
        var exception = new NotSupportedException("Operation not supported");

        // Act
        var result = _mapper.Map<string>(exception);

        // Assert
        result.StatusCode.Should().Be(ResultStatusCode.OperationNotSupported);
    }

    [Fact]
    public void Map_KeyNotFoundException_ShouldReturnNotFoundStatusCode()
    {
        // Arrange
        var exception = new KeyNotFoundException("Key not found");

        // Act
        var result = _mapper.Map<string>(exception);

        // Assert
        result.StatusCode.Should().Be(ResultStatusCode.NotFound);
    }

    [Fact]
    public void Map_FormatException_ShouldReturnInvalidFormatStatusCode()
    {
        // Arrange
        var exception = new FormatException("Invalid format");

        // Act
        var result = _mapper.Map<string>(exception);

        // Assert
        result.StatusCode.Should().Be(ResultStatusCode.InvalidFormat);
    }

    [Fact]
    public void Map_UnknownException_ShouldReturnInternalErrorStatusCode()
    {
        // Arrange
        var exception = new Exception("Unknown error");

        // Act
        var result = _mapper.Map<string>(exception);

        // Assert
        result.StatusCode.Should().Be(ResultStatusCode.InternalError);
    }

    [Fact]
    public void Map_WithCustomMessage_ShouldUseCustomMessage()
    {
        // Arrange
        var exception = new NotFoundException("Original message");
        var customMessage = "Custom error message";

        // Act
        var result = _mapper.Map<string>(exception, customMessage);

        // Assert
        result.Errors.Should().Contain(e => e.Message == customMessage);
    }

    [Fact]
    public void Map_WithNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => _mapper.Map<string>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region [ GetStatusCode Tests ]

    [Fact]
    public void GetStatusCode_NotFoundException_ShouldReturnNotFound()
    {
        // Arrange
        var exception = new NotFoundException("Not found");

        // Act
        var statusCode = _mapper.GetStatusCode(exception);

        // Assert
        statusCode.Should().Be(ResultStatusCode.NotFound);
    }

    [Fact]
    public void GetStatusCode_UnknownException_ShouldReturnInternalError()
    {
        // Arrange
        var exception = new Exception("Unknown");

        // Act
        var statusCode = _mapper.GetStatusCode(exception);

        // Assert
        statusCode.Should().Be(ResultStatusCode.InternalError);
    }

    #endregion

    #region [ GetErrorCode Tests ]

    [Fact]
    public void GetErrorCode_NotFoundException_ShouldReturnResourceNotFound()
    {
        // Arrange
        var exception = new NotFoundException("Not found");

        // Act
        var errorCode = _mapper.GetErrorCode(exception);

        // Assert
        errorCode.Should().Be("RESOURCE.NOT_FOUND");
    }

    [Fact]
    public void GetErrorCode_UnauthorizedException_ShouldReturnAuthUnauthorized()
    {
        // Arrange
        var exception = new UnauthorizedException("Not authorized");

        // Act
        var errorCode = _mapper.GetErrorCode(exception);

        // Assert
        errorCode.Should().Be("AUTH.UNAUTHORIZED");
    }

    [Fact]
    public void GetErrorCode_UnknownException_ShouldReturnInternalError()
    {
        // Arrange
        var exception = new Exception("Unknown");

        // Act
        var errorCode = _mapper.GetErrorCode(exception);

        // Assert
        errorCode.Should().Be("SYSTEM.INTERNAL_ERROR");
    }

    #endregion

    #region [ ShouldLog Tests ]

    [Fact]
    public void ShouldLog_NotFoundException_ShouldReturnFalse()
    {
        // Arrange
        var exception = new NotFoundException("Not found");

        // Act
        var shouldLog = _mapper.ShouldLog(exception);

        // Assert
        shouldLog.Should().BeFalse();
    }

    [Fact]
    public void ShouldLog_TimeoutException_ShouldReturnTrue()
    {
        // Arrange
        var exception = new TimeoutException("Timeout");

        // Act
        var shouldLog = _mapper.ShouldLog(exception);

        // Assert
        shouldLog.Should().BeTrue();
    }

    [Fact]
    public void ShouldLog_NullReferenceException_ShouldReturnTrue()
    {
        // Arrange
        var exception = new NullReferenceException("Null reference");

        // Act
        var shouldLog = _mapper.ShouldLog(exception);

        // Assert
        shouldLog.Should().BeTrue();
    }

    #endregion

    #region [ Custom Mapping Tests ]

    [Fact]
    public void Map_WithCustomMapping_ShouldUseCustomMapping()
    {
        // Arrange
        var customOptions = new ExceptionMappingOptions();
        customOptions.CustomMappings[typeof(CustomTestException)] = new ExceptionMapping
        {
            StatusCode = ResultStatusCode.RateLimitExceeded,
            ErrorCode = "CUSTOM.TEST_ERROR",
            ShouldLog = true
        };

        var mapper = new ExceptionToResultMapper(Options.Create(customOptions));
        var exception = new CustomTestException("Custom exception");

        // Act
        var result = mapper.Map<string>(exception);

        // Assert
        result.StatusCode.Should().Be(ResultStatusCode.RateLimitExceeded);
        result.PrimaryErrorCode.Should().Be("CUSTOM.TEST_ERROR");
    }

    [Fact]
    public void ShouldIncludeDetails_WithDefaultOptions_ShouldReturnFalse()
    {
        // Arrange
        var exception = new Exception("Test");

        // Act
        var includeDetails = _mapper.ShouldIncludeDetails(exception);

        // Assert
        includeDetails.Should().BeFalse();
    }

    [Fact]
    public void Map_WithDetailsEnabled_ShouldIncludeInnerException()
    {
        // Arrange
        var options = new ExceptionMappingOptions
        {
            IncludeExceptionDetails = true
        };
        var mapper = new ExceptionToResultMapper(Options.Create(options));
        var innerException = new InvalidOperationException("Inner error");
        var exception = new Exception("Outer error", innerException);

        // Act
        var result = mapper.Map<string>(exception);

        // Assert
        result.Infos.Should().Contain(i => i.Message.Contains("Inner error"));
    }

    [Fact]
    public void Map_WithStackTraceEnabled_ShouldIncludeStackTrace()
    {
        // Arrange
        var options = new ExceptionMappingOptions
        {
            IncludeExceptionDetails = true,
            IncludeStackTrace = true
        };
        var mapper = new ExceptionToResultMapper(Options.Create(options));

        Exception exception;
        try
        {
            throw new InvalidOperationException("Test exception");
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // Act
        var result = mapper.Map<string>(exception);

        // Assert - ResultMessage.Info uses Key parameter, not ErrorCode
        result.Infos.Should().Contain(i => i.Key == "EXCEPTION.STACK_TRACE");
    }

    #endregion

    #region [ Inheritance Mapping Tests ]

    [Fact]
    public void Map_DerivedFromMappedException_ShouldUseMappingFromBaseType()
    {
        // Arrange
        var exception = new DerivedNotFoundException("Derived not found");

        // Act
        var result = _mapper.Map<string>(exception);

        // Assert
        result.StatusCode.Should().Be(ResultStatusCode.NotFound);
    }

    #endregion

    #region [ Test Support Classes ]

    private class CustomTestException : Exception
    {
        public CustomTestException(string message) : base(message) { }
    }

    private class DerivedNotFoundException : NotFoundException
    {
        public DerivedNotFoundException(string message) : base(message) { }
    }

    #endregion
}

