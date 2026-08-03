using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.ValueObjects.Logic;

namespace Mvp24Hours.Core.Test.ValueObjects;

[Trait("Category", "Unit")]
public class StructuredMessageResultTest
{
    [Fact]
    public void Validation_Should_SetValidationDefaults()
    {
        StructuredMessageResult result = StructuredMessageResult.Validation("Email", "Invalid email");

        result.Key.Should().Be("Email");
        result.ErrorCode.Should().Be("VALIDATION_FAILED");
        result.Category.Should().Be(ErrorCategory.Validation);
        result.HttpStatusCode.Should().Be(400);
        result.PropertyName.Should().Be("Email");
    }

    [Fact]
    public void NotFound_WithId_Should_FormatMessageAndCode()
    {
        StructuredMessageResult result = StructuredMessageResult.NotFound("User", 99);

        result.Message.Should().Contain("99");
        result.ErrorCode.Should().Be("USER_NOT_FOUND");
        result.HttpStatusCode.Should().Be(404);
        result.Details.Should().NotBeNull();
    }

    [Fact]
    public void NotFound_WithoutId_Should_UseResourceName()
    {
        StructuredMessageResult result = StructuredMessageResult.NotFound("Invoice");

        result.Message.Should().Contain("Invoice");
        result.ErrorCode.Should().Be("INVOICE_NOT_FOUND");
    }

    [Fact]
    public void BusinessError_Should_Set422Status()
    {
        StructuredMessageResult result = StructuredMessageResult.BusinessError("LIMIT", "Exceeded", new { Limit = 10 });

        result.Category.Should().Be(ErrorCategory.Business);
        result.HttpStatusCode.Should().Be(422);
        result.Details.Should().NotBeNull();
    }

    [Fact]
    public void Forbidden_Should_IncludeActionInMessage()
    {
        StructuredMessageResult result = StructuredMessageResult.Forbidden("Orders", "delete");

        result.Message.Should().Contain("delete");
        result.ErrorCode.Should().Be("FORBIDDEN");
        result.HttpStatusCode.Should().Be(403);
    }

    [Fact]
    public void Unauthorized_Should_UseDefaultMessage()
    {
        StructuredMessageResult result = StructuredMessageResult.Unauthorized();

        result.ErrorCode.Should().Be("UNAUTHORIZED");
        result.HttpStatusCode.Should().Be(401);
    }

    [Fact]
    public void Conflict_Should_Set409Status()
    {
        StructuredMessageResult result = StructuredMessageResult.Conflict("Order", "Already exists");

        result.ErrorCode.Should().Be("ORDER_CONFLICT");
        result.HttpStatusCode.Should().Be(409);
    }

    [Fact]
    public void SystemError_Should_Set500Status()
    {
        StructuredMessageResult result = StructuredMessageResult.SystemError("Unexpected failure");

        result.Category.Should().Be(ErrorCategory.System);
        result.HttpStatusCode.Should().Be(500);
    }

    [Fact]
    public void InfoAndWarning_Should_SetMessageTypes()
    {
        StructuredMessageResult info = StructuredMessageResult.Info("saved", "Saved successfully");
        StructuredMessageResult warning = StructuredMessageResult.Warning("slow", "Slow response", "SLOW");

        info.Type.Should().Be(MessageType.Info);
        warning.Type.Should().Be(MessageType.Warning);
        warning.ErrorCode.Should().Be("SLOW");
    }

    [Fact]
    public void Constructor_Should_DefaultUnknownErrorCode()
    {
        var result = new StructuredMessageResult("key", "message", null!);

        result.ErrorCode.Should().Be("UNKNOWN_ERROR");
    }

    [Fact]
    public void Equality_Should_BeValueBased()
    {
        var left = new StructuredMessageResult("k", "m", "E1", MessageType.Error, ErrorCategory.Validation);
        var right = new StructuredMessageResult("k", "m", "E1", MessageType.Error, ErrorCategory.Validation);

        left.Should().Be(right);
    }
}
