//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.ValueObjects.Logic;

namespace Mvp24Hours.Core.Test.ValueObjects;

/// <summary>
/// Unit tests for Logic Value Objects: BusinessResult, MessageResult, PagingCriteria, BusinessResultFactory.
/// </summary>
[Trait("Category", "Unit")]
public class LogicValueObjectsTest
{
    #region MessageResult Tests

    [Fact]
    public void MessageResult_Create_WithMessageAndType_StoresValues()
    {
        // Act
        var message = new MessageResult("Error occurred", MessageType.Error);

        // Assert
        message.Message.Should().Be("Error occurred");
        message.Type.Should().Be(MessageType.Error);
        message.Key.Should().BeNull();
        message.CustomType.Should().BeEmpty();
    }

    [Fact]
    public void MessageResult_Create_WithKey_StoresKey()
    {
        // Act
        var message = new MessageResult("field_name", "Field is required", MessageType.Error);

        // Assert
        message.Key.Should().Be("field_name");
        message.Message.Should().Be("Field is required");
        message.Type.Should().Be(MessageType.Error);
    }

    [Fact]
    public void MessageResult_Create_WithCustomType_StoresCustomType()
    {
        // Act
        var message = new MessageResult("Custom message", "MY_CUSTOM_TYPE");

        // Assert
        message.Message.Should().Be("Custom message");
        message.Type.Should().Be(MessageType.Custom);
        message.CustomType.Should().Be("MY_CUSTOM_TYPE");
    }

    [Fact]
    public void MessageResult_Create_WithAllParams_StoresAllValues()
    {
        // Act
        var message = new MessageResult("key", "message", MessageType.Warning, "MY_TYPE");

        // Assert
        message.Key.Should().Be("key");
        message.Message.Should().Be("message");
        message.Type.Should().Be(MessageType.Warning);
        message.CustomType.Should().Be("MY_TYPE");
    }

    [Fact]
    public void MessageResult_Equality_SameValues_AreEqual()
    {
        // Arrange
        var msg1 = new MessageResult("key", "message", MessageType.Error);
        var msg2 = new MessageResult("key", "message", MessageType.Error);

        // Assert
        msg1.Should().Be(msg2);
    }

    [Fact]
    public void MessageResult_Equality_DifferentMessages_AreNotEqual()
    {
        // Arrange
        var msg1 = new MessageResult("Error A", MessageType.Error);
        var msg2 = new MessageResult("Error B", MessageType.Error);

        // Assert
        msg1.Should().NotBe(msg2);
    }

    [Fact]
    public void MessageResult_AllMessageTypes_CanBeCreated()
    {
        // Assert
        var info = new MessageResult("Info message", MessageType.Info);
        var warning = new MessageResult("Warning message", MessageType.Warning);
        var error = new MessageResult("Error message", MessageType.Error);
        var success = new MessageResult("Success message", MessageType.Success);

        info.Type.Should().Be(MessageType.Info);
        warning.Type.Should().Be(MessageType.Warning);
        error.Type.Should().Be(MessageType.Error);
        success.Type.Should().Be(MessageType.Success);
    }

    #endregion

    #region BusinessResult<T> Tests

    [Fact]
    public void BusinessResult_Create_WithData_StoresData()
    {
        // Act
        var result = new BusinessResult<string>("test data");

        // Assert
        result.Data.Should().Be("test data");
        result.HasErrors.Should().BeFalse();
        result.IsSuccess.Should().BeTrue();
        result.Messages.Should().BeNull();
        result.Token.Should().BeNull();
    }

    [Fact]
    public void BusinessResult_Create_WithMessages_StoresMessages()
    {
        // Arrange
        var messages = new List<Mvp24Hours.Core.Contract.ValueObjects.Logic.IMessageResult>
        {
            new MessageResult("Error occurred", MessageType.Error)
        }.AsReadOnly();

        // Act
        var result = new BusinessResult<int>(42, messages);

        // Assert
        result.Data.Should().Be(42);
        result.Messages.Should().HaveCount(1);
        result.HasErrors.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void BusinessResult_Create_WithToken_StoresToken()
    {
        // Act
        var result = new BusinessResult<string>("data", null, "my-token");

        // Assert
        result.Token.Should().Be("my-token");
    }

    [Fact]
    public void BusinessResult_SetToken_WhenTokenIsEmpty_SetsToken()
    {
        // Arrange
        var result = new BusinessResult<string>("data");

        // Act
        result.SetToken("new-token");

        // Assert
        result.Token.Should().Be("new-token");
    }

    [Fact]
    public void BusinessResult_SetToken_WhenTokenAlreadySet_DoesNotOverwrite()
    {
        // Arrange
        var result = new BusinessResult<string>("data", null, "original-token");

        // Act
        result.SetToken("new-token");

        // Assert
        result.Token.Should().Be("original-token");
    }

    [Fact]
    public void BusinessResult_ImplicitConversion_FromData_CreatesSuccessResult()
    {
        // Act
        BusinessResult<string> result = "test data";

        // Assert
        result.Data.Should().Be("test data");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void BusinessResult_ImplicitConversion_ToData_ExtractsData()
    {
        // Arrange
        var result = new BusinessResult<int>(42);

        // Act
        int? data = result;

        // Assert
        data.Should().Be(42);
    }

    [Fact]
    public void BusinessResult_ImplicitConversion_ToBool_ReturnsTrueForSuccess()
    {
        // Arrange
        var result = new BusinessResult<string>("data");

        // Act
        bool isSuccess = result;

        // Assert
        isSuccess.Should().BeTrue();
    }

    [Fact]
    public void BusinessResult_ImplicitConversion_ToBool_ReturnsFalseForError()
    {
        // Arrange
        var messages = new List<Mvp24Hours.Core.Contract.ValueObjects.Logic.IMessageResult>
        {
            new MessageResult("Error", MessageType.Error)
        }.AsReadOnly();
        var result = new BusinessResult<string>(null, messages);

        // Act
        bool isSuccess = result;

        // Assert
        isSuccess.Should().BeFalse();
    }

    [Fact]
    public void BusinessResult_ImplicitConversion_NullToBool_ReturnsFalse()
    {
        // Arrange
        BusinessResult<string>? result = null;

        // Act
        bool isSuccess = result;

        // Assert
        isSuccess.Should().BeFalse();
    }

    #endregion

    #region BusinessResult (Factory) Tests

    [Fact]
    public void BusinessResult_Success_WithData_CreatesSuccessResult()
    {
        // Act
        var result = BusinessResult.Success("test data");

        // Assert
        result.Data.Should().Be("test data");
        result.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void BusinessResult_Success_WithDataAndMessage_CreatesResultWithInfoMessage()
    {
        // Act - using named parameter to force the overload with message
        var result = BusinessResult.Success(data: 42, message: "Operation completed");

        // Assert
        result.Data.Should().Be(42);
        result.HasErrors.Should().BeFalse();
        result.Messages.Should().HaveCount(1);
        result.Messages!.First().Type.Should().Be(MessageType.Info);
    }

    [Fact]
    public void BusinessResult_Success_WithToken_IncludesToken()
    {
        // Act
        var result = BusinessResult.Success("data", token: "my-token");

        // Assert
        result.Token.Should().Be("my-token");
    }

    [Fact]
    public void BusinessResult_Failure_WithErrorMessage_CreatesFailureResult()
    {
        // Act
        var result = BusinessResult.Failure<string>("Something went wrong");

        // Assert
        result.HasErrors.Should().BeTrue();
        result.Data.Should().BeNull();
        result.Messages.Should().HaveCount(1);
        result.Messages!.First().Message.Should().Be("Something went wrong");
        result.Messages.First().Type.Should().Be(MessageType.Error);
    }

    [Fact]
    public void BusinessResult_Failure_WithErrorKey_IncludesKey()
    {
        // Act
        var result = BusinessResult.Failure<string>("Error message", "ERROR_CODE");

        // Assert
        result.Messages!.First().Key.Should().Be("ERROR_CODE");
    }

    [Fact]
    public void BusinessResult_Failure_FromException_UsesExceptionMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception message");

        // Act
        var result = BusinessResult.Failure<string>(exception);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.Messages!.First().Message.Should().Be("Test exception message");
        result.Messages!.First().Key.Should().Be("InvalidOperationException");
    }

    [Fact]
    public void BusinessResult_Failure_WithMultipleErrors_ContainsAllErrors()
    {
        // Arrange
        var errors = new List<(string key, string message)>
        {
            ("FIELD_A", "Field A is required"),
            ("FIELD_B", "Field B is invalid")
        };

        // Act
        var result = BusinessResult.Failure<string>(errors);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.Messages.Should().HaveCount(2);
    }

    [Fact]
    public void BusinessResult_From_WithData_CreatesSuccessResult()
    {
        // Arrange
        var order = new { Id = 1, Name = "Test Order" };

        // Act
        var result = BusinessResult.From(order);

        // Assert
        result.HasErrors.Should().BeFalse();
        result.Data.Should().Be(order);
    }

    [Fact]
    public void BusinessResult_From_WithNull_CreatesFailureResult()
    {
        // Arrange
        string? nullData = null;

        // Act
        var result = BusinessResult.From(nullData, "Resource not found");

        // Assert
        result.HasErrors.Should().BeTrue();
        result.Messages!.First().Message.Should().Be("Resource not found");
    }

    [Fact]
    public void BusinessResult_FromCondition_WhenTrue_CreatesSuccessResult()
    {
        // Act
        var result = BusinessResult.FromCondition(true, "success-data");

        // Assert
        result.HasErrors.Should().BeFalse();
        result.Data.Should().Be("success-data");
    }

    [Fact]
    public void BusinessResult_FromCondition_WhenFalse_CreatesFailureResult()
    {
        // Act
        var result = BusinessResult.FromCondition(false, "success-data", "Condition failed");

        // Assert
        result.HasErrors.Should().BeTrue();
        result.Messages!.First().Message.Should().Be("Condition failed");
    }

    [Fact]
    public void BusinessResult_Combine_AllSuccess_ReturnsTrueResult()
    {
        // Arrange
        var result1 = BusinessResult.Success("data1");
        var result2 = BusinessResult.Success(42);

        // Act
        var combined = BusinessResult.Combine(result1, result2);

        // Assert
        combined.Data.Should().BeTrue();
        combined.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void BusinessResult_Combine_WithFailure_ReturnsErrorResult()
    {
        // Arrange
        var success = BusinessResult.Success("data");
        var failure = BusinessResult.Failure<string>("Error occurred");

        // Act
        var combined = BusinessResult.Combine(success, failure);

        // Assert
        combined.HasErrors.Should().BeTrue();
    }

    #endregion

    #region PagingCriteria Tests

    [Fact]
    public void PagingCriteria_Create_WithBasicParams_StoresValues()
    {
        // Act
        var paging = new PagingCriteria(10, 0);

        // Assert
        paging.Limit.Should().Be(10);
        paging.Offset.Should().Be(0);
        paging.OrderBy.Should().BeNull();
        paging.Navigation.Should().BeNull();
    }

    [Fact]
    public void PagingCriteria_Create_WithOrderBy_StoresOrderBy()
    {
        // Arrange
        var orderBy = new List<string> { "Name asc", "CreatedAt desc" };

        // Act
        var paging = new PagingCriteria(10, 0, orderBy);

        // Assert
        paging.OrderBy.Should().BeEquivalentTo(orderBy);
    }

    [Fact]
    public void PagingCriteria_Create_WithNavigation_StoresNavigation()
    {
        // Arrange
        var navigation = new List<string> { "Orders", "Address" };

        // Act
        var paging = new PagingCriteria(10, 0, null, navigation);

        // Assert
        paging.Navigation.Should().BeEquivalentTo(navigation);
    }

    [Fact]
    public void PagingCriteria_Equality_SameCriteria_AreEqual()
    {
        // Arrange
        var paging1 = new PagingCriteria(10, 0);
        var paging2 = new PagingCriteria(10, 0);

        // Assert
        paging1.Should().Be(paging2);
    }

    [Fact]
    public void PagingCriteria_Equality_DifferentLimits_AreNotEqual()
    {
        // Arrange
        var paging1 = new PagingCriteria(10, 0);
        var paging2 = new PagingCriteria(20, 0);

        // Assert
        paging1.Should().NotBe(paging2);
    }

    [Fact]
    public void PagingCriteria_WithLargeOffset_StoresCorrectly()
    {
        // Act
        var paging = new PagingCriteria(10, 100);

        // Assert
        paging.Limit.Should().Be(10);
        paging.Offset.Should().Be(100);
    }

    #endregion
}
