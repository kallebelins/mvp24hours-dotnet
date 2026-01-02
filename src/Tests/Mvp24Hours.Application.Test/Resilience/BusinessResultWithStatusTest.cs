//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentAssertions;
using Mvp24Hours.Application.Contract.Resilience;
using Mvp24Hours.Application.Logic.Resilience;
using Xunit;

namespace Mvp24Hours.Application.Test.Resilience;

/// <summary>
/// Unit tests for BusinessResultWithStatus functionality.
/// </summary>
public class BusinessResultWithStatusTest
{
    #region [ Factory Method Tests - Success ]

    [Fact]
    public void Success_WithData_ShouldReturnSuccessResult()
    {
        // Arrange
        var data = "Test data";

        // Act
        var result = BusinessResultWithStatus.Success(data);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.StatusCode.Should().Be(ResultStatusCode.Success);
        result.Data.Should().Be(data);
    }

    [Fact]
    public void Success_WithDataAndInfoMessage_ShouldReturnSuccessResultWithMessage()
    {
        // Arrange
        var data = 42;
        var infoMessage = "Operation completed successfully";

        // Act - Using named parameter to ensure we call the correct overload
        var result = BusinessResultWithStatus.Success(data, infoMessage: infoMessage);

        // Assert
        result.HasErrors.Should().BeFalse();
        result.Data.Should().Be(data);
        result.Infos.Should().Contain(i => i.Message == infoMessage);
    }

    [Fact]
    public void Success_WithToken_ShouldReturnResultWithToken()
    {
        // Arrange
        var data = new TestEntity { Id = 1, Name = "Test" };
        var token = "test-token-123";

        // Act
        var result = BusinessResultWithStatus.Success(data, token);

        // Assert
        result.HasErrors.Should().BeFalse();
        result.Token.Should().Be(token);
        result.Data.Should().Be(data);
    }

    [Fact]
    public void SuccessWithWarning_ShouldReturnSuccessWithWarning()
    {
        // Arrange
        var data = "Test";
        var warningMessage = "This is a warning";

        // Act
        var result = BusinessResultWithStatus.SuccessWithWarning(data, warningMessage);

        // Assert
        result.HasErrors.Should().BeFalse();
        result.HasWarnings.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Message == warningMessage);
    }

    [Fact]
    public void SuccessWithWarnings_ShouldReturnSuccessWithMultipleWarnings()
    {
        // Arrange
        var data = "Test";
        var warnings = new List<IResultMessage>
        {
            ResultMessage.Warning("Warning 1"),
            ResultMessage.Warning("Warning 2")
        };

        // Act
        var result = BusinessResultWithStatus.SuccessWithWarnings(data, warnings);

        // Assert
        result.HasErrors.Should().BeFalse();
        result.HasWarnings.Should().BeTrue();
        result.Warnings.Should().HaveCount(2);
    }

    #endregion

    #region [ Factory Method Tests - Failure ]

    [Fact]
    public void Failure_WithStatusCodeAndMessage_ShouldReturnFailureResult()
    {
        // Arrange
        var errorMessage = "Operation failed";

        // Act
        var result = BusinessResultWithStatus.Failure<string>(
            ResultStatusCode.InternalError,
            errorMessage);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.StatusCode.Should().Be(ResultStatusCode.InternalError);
        result.Errors.Should().Contain(e => e.Message == errorMessage);
    }

    [Fact]
    public void Failure_WithStatusCodeAndErrorCode_ShouldReturnDetailedFailure()
    {
        // Arrange
        var errorMessage = "Resource not found";
        var errorCode = "RESOURCE.NOT_FOUND";
        var statusCode = ResultStatusCode.NotFound;

        // Act
        var result = BusinessResultWithStatus.Failure<string>(
            statusCode,
            errorMessage,
            errorCode);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.StatusCode.Should().Be(statusCode);
        result.PrimaryErrorCode.Should().Be(errorCode);
        result.Errors.Should().Contain(e => e.Message == errorMessage);
    }

    [Fact]
    public void Failure_WithMultipleMessages_ShouldReturnAllErrors()
    {
        // Arrange
        var errors = new List<IResultMessage>
        {
            ResultMessage.Error("Error 1"),
            ResultMessage.Error("Error 2"),
            ResultMessage.Error("Error 3")
        };

        // Act
        var result = BusinessResultWithStatus.Failure<string>(
            ResultStatusCode.ValidationFailed,
            errors);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.Errors.Should().HaveCount(3);
    }

    [Fact]
    public void NotFound_WithMessage_ShouldReturnNotFoundResult()
    {
        // Arrange
        var message = "Entity not found";

        // Act
        var result = BusinessResultWithStatus.NotFound<string>(message);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.StatusCode.Should().Be(ResultStatusCode.NotFound);
        result.Errors.Should().Contain(e => e.Message == message);
    }

    [Fact]
    public void NotFound_WithEntityAndId_ShouldReturnDescriptiveMessage()
    {
        // Arrange
        var entityName = "Customer";
        var id = 123;

        // Act
        var result = BusinessResultWithStatus.NotFound<string>(entityName, id);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.StatusCode.Should().Be(ResultStatusCode.NotFound);
        result.Errors.Should().Contain(e => e.Message.Contains(entityName) && e.Message.Contains(id.ToString()));
    }

    [Fact]
    public void ValidationFailed_WithMessage_ShouldReturnValidationFailedResult()
    {
        // Arrange
        var message = "Validation failed";

        // Act
        var result = BusinessResultWithStatus.ValidationFailed<string>(message);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.StatusCode.Should().Be(ResultStatusCode.ValidationFailed);
        result.Errors.Should().Contain(e => e.Message == message);
    }

    [Fact]
    public void ValidationFailed_WithMultipleErrors_ShouldReturnAllValidationErrors()
    {
        // Arrange
        var validationErrors = new (string propertyName, string message, string? errorCode)[]
        {
            ("Name", "Name is required", null),
            ("Email", "Email is invalid", "VALIDATION.INVALID_EMAIL")
        };

        // Act
        var result = BusinessResultWithStatus.ValidationFailed<string>(validationErrors);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.StatusCode.Should().Be(ResultStatusCode.ValidationFailed);
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void Unauthorized_WithMessage_ShouldReturnUnauthorizedResult()
    {
        // Arrange
        var message = "Authentication required";

        // Act
        var result = BusinessResultWithStatus.Unauthorized<string>(message);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.StatusCode.Should().Be(ResultStatusCode.Unauthorized);
    }

    [Fact]
    public void Forbidden_WithMessage_ShouldReturnForbiddenResult()
    {
        // Arrange
        var message = "Access denied";

        // Act
        var result = BusinessResultWithStatus.Forbidden<string>(message);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.StatusCode.Should().Be(ResultStatusCode.Forbidden);
    }

    [Fact]
    public void Conflict_WithMessage_ShouldReturnConflictResult()
    {
        // Arrange
        var message = "Resource conflict";

        // Act
        var result = BusinessResultWithStatus.Conflict<string>(message);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.StatusCode.Should().Be(ResultStatusCode.Conflict);
    }

    [Fact]
    public void InternalError_WithMessage_ShouldReturnInternalErrorResult()
    {
        // Arrange
        var message = "Internal server error";

        // Act
        var result = BusinessResultWithStatus.InternalError<string>(message);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.StatusCode.Should().Be(ResultStatusCode.InternalError);
    }

    [Fact]
    public void DomainRuleViolation_WithMessage_ShouldReturnDomainRuleViolationResult()
    {
        // Arrange
        var message = "Domain rule violated";

        // Act
        var result = BusinessResultWithStatus.DomainRuleViolation<string>(message);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.StatusCode.Should().Be(ResultStatusCode.DomainRuleViolation);
    }

    #endregion

    #region [ From Methods Tests ]

    [Fact]
    public void From_WithNonNullData_ShouldReturnSuccess()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test" };

        // Act
        var result = BusinessResultWithStatus.From(entity);

        // Assert
        result.HasErrors.Should().BeFalse();
        result.Data.Should().Be(entity);
    }

    [Fact]
    public void From_WithNullData_ShouldReturnNotFound()
    {
        // Act
        var result = BusinessResultWithStatus.From<TestEntity>(null);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.StatusCode.Should().Be(ResultStatusCode.NotFound);
    }

    [Fact]
    public void FromValue_WithValue_ShouldReturnSuccess()
    {
        // Arrange
        int? value = 42;

        // Act
        var result = BusinessResultWithStatus.FromValue(value);

        // Assert
        result.HasErrors.Should().BeFalse();
        result.Data.Should().Be(42);
    }

    [Fact]
    public void FromValue_WithNull_ShouldReturnNotFound()
    {
        // Arrange
        int? value = null;

        // Act
        var result = BusinessResultWithStatus.FromValue(value);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.StatusCode.Should().Be(ResultStatusCode.NotFound);
    }

    #endregion

    #region [ Instance Properties Tests ]

    [Fact]
    public void Constructor_WithData_ShouldStoreData()
    {
        // Arrange & Act
        var result = new BusinessResultWithStatus<string>("data");

        // Assert
        result.Data.Should().Be("data");
    }

    [Fact]
    public void Constructor_WithNullData_ShouldStoreNull()
    {
        // Arrange & Act
        var result = new BusinessResultWithStatus<string?>(null);

        // Assert
        result.Data.Should().BeNull();
    }

    [Fact]
    public void HasWarnings_WithWarning_ShouldReturnTrue()
    {
        // Arrange
        var messages = new List<IResultMessage> { ResultMessage.Warning("Warning") };
        var result = new BusinessResultWithStatus<string>("data", messages: messages);

        // Assert
        result.HasWarnings.Should().BeTrue();
    }

    [Fact]
    public void HasWarnings_WithoutWarnings_ShouldReturnFalse()
    {
        // Arrange
        var result = new BusinessResultWithStatus<string>("data");

        // Assert
        result.HasWarnings.Should().BeFalse();
    }

    [Fact]
    public void Infos_WithInfoMessage_ShouldContainMessage()
    {
        // Arrange
        var messages = new List<IResultMessage> { ResultMessage.Info("Info") };
        var result = new BusinessResultWithStatus<string>("data", messages: messages);

        // Assert
        result.Infos.Should().NotBeEmpty();
    }

    [Fact]
    public void HasErrorCode_WithMatchingCode_ShouldReturnTrue()
    {
        // Arrange
        var messages = new List<IResultMessage> { ResultMessage.Error("Error", "TEST.ERROR") };
        var result = new BusinessResultWithStatus<string>(null, ResultStatusCode.InternalError, messages);

        // Assert
        result.HasErrorCode("TEST.ERROR").Should().BeTrue();
    }

    [Fact]
    public void HasErrorCode_WithNonMatchingCode_ShouldReturnFalse()
    {
        // Arrange
        var messages = new List<IResultMessage> { ResultMessage.Error("Error", "TEST.ERROR") };
        var result = new BusinessResultWithStatus<string>(null, ResultStatusCode.InternalError, messages);

        // Assert
        result.HasErrorCode("OTHER.ERROR").Should().BeFalse();
    }

    [Fact]
    public void HasPropertyError_WithMatchingProperty_ShouldReturnTrue()
    {
        // Arrange
        var messages = new List<IResultMessage> { ResultMessage.Error("Error", "ERROR", "Name") };
        var result = new BusinessResultWithStatus<string>(null, ResultStatusCode.ValidationFailed, messages);

        // Assert
        result.HasPropertyError("Name").Should().BeTrue();
    }

    #endregion

    #region [ Token Tests ]

    [Fact]
    public void SetToken_WithValidToken_ShouldSetToken()
    {
        // Arrange
        var result = new BusinessResultWithStatus<string>("data");
        var token = "new-token";

        // Act
        result.SetToken(token);

        // Assert
        result.Token.Should().Be(token);
    }

    [Fact]
    public void SetToken_WhenTokenAlreadySet_ShouldNotOverwrite()
    {
        // Arrange
        var result = new BusinessResultWithStatus<string>("data", token: "original-token");
        var newToken = "new-token";

        // Act
        result.SetToken(newToken);

        // Assert
        result.Token.Should().Be("original-token");
    }

    #endregion

    #region [ Implicit Conversion Tests ]

    [Fact]
    public void ImplicitConversion_DataToResult_ShouldCreateSuccessResult()
    {
        // Act
        BusinessResultWithStatus<string> result = "test data";

        // Assert
        result.HasErrors.Should().BeFalse();
        result.Data.Should().Be("test data");
    }

    [Fact]
    public void ImplicitConversion_ResultToData_ShouldReturnData()
    {
        // Arrange
        var result = new BusinessResultWithStatus<string>("test data");

        // Act
        string? data = result;

        // Assert
        data.Should().Be("test data");
    }

    [Fact]
    public void ImplicitConversion_ResultToBool_SuccessResult_ShouldReturnTrue()
    {
        // Arrange
        var successResult = new BusinessResultWithStatus<string>("data");

        // Act & Assert
        ((bool)successResult).Should().BeTrue();
    }

    [Fact]
    public void ImplicitConversion_ResultToBool_FailureResult_ShouldReturnFalse()
    {
        // Arrange
        var failureResult = new BusinessResultWithStatus<string>(
            null,
            ResultStatusCode.InternalError,
            new[] { ResultMessage.Error("Error") });

        // Act & Assert
        ((bool)failureResult).Should().BeFalse();
    }

    #endregion

    #region [ ToString Tests ]

    [Fact]
    public void ToString_SuccessResult_ShouldContainSuccessInfo()
    {
        // Arrange
        var result = new BusinessResultWithStatus<string>("data");

        // Act
        var str = result.ToString();

        // Assert
        str.Should().Contain("Success");
        str.Should().Contain("String");
    }

    [Fact]
    public void ToString_FailureResult_ShouldContainFailureInfo()
    {
        // Arrange
        var result = new BusinessResultWithStatus<string>(
            null,
            ResultStatusCode.NotFound,
            new[] { ResultMessage.Error("Error") });

        // Act
        var str = result.ToString();

        // Assert
        str.Should().Contain("Failure");
        str.Should().Contain("NotFound");
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
