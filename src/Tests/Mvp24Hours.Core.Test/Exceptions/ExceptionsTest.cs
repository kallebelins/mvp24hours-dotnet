using System.Net;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.Core.ValueObjects.Logic;

namespace Mvp24Hours.Core.Test.Exceptions;

[Trait("Category", "Unit")]
public class ExceptionsTest
{
    [Fact]
    public void Mvp24HoursException_StoresErrorCodeAndContext()
    {
        var context = new Dictionary<string, object> { ["Key"] = "Value" };
        var exception = new Mvp24HoursException("failed", "ERR", context);

        exception.Message.Should().Be("failed");
        exception.ErrorCode.Should().Be("ERR");
        exception.Context.Should().ContainKey("Key");
    }

    [Fact]
    public void NotFoundException_For_SetsEntityMetadata()
    {
        var exception = NotFoundException.For<SampleEntity>(42);

        exception.EntityName.Should().Be(nameof(SampleEntity));
        exception.EntityId.Should().Be(42);
        exception.ErrorCode.Should().Be("NOT_FOUND");
    }

    [Fact]
    public void UnauthorizedException_FactoryMethods_SetExpectedMessages()
    {
        var expired = UnauthorizedException.TokenExpired();
        var invalid = UnauthorizedException.InvalidCredentials();

        expired.AuthenticationScheme.Should().Be("Bearer");
        expired.Message.Should().Contain("expired");
        invalid.ErrorCode.Should().Be("UNAUTHORIZED");
    }

    [Fact]
    public void ForbiddenException_FactoryMethods_SetMetadata()
    {
        var missingRole = ForbiddenException.MissingRole("Admin");
        var notOwner = ForbiddenException.NotOwner<SampleEntity>(99);

        missingRole.RequiredPermission.Should().Be("Admin");
        notOwner.ResourceName.Should().Be(nameof(SampleEntity));
    }

    [Fact]
    public void ValidationException_StoresValidationErrors()
    {
        IList<IMessageResult> errors = [new MessageResult("Name", "Required", MessageType.Error)];
        var exception = new ValidationException("Validation failed", errors);

        exception.ValidationErrors.Should().HaveCount(1);
    }

    [Fact]
    public void ConflictException_FactoryMethods_SetConflictDetails()
    {
        var duplicate = ConflictException.Duplicate<SampleEntity>("Code", "ABC");
        var concurrency = ConflictException.ConcurrencyConflict<SampleEntity>(1);

        duplicate.PropertyName.Should().Be("Code");
        concurrency.PropertyName.Should().Be("Version");
    }

    [Fact]
    public void DomainException_FactoryMethods_SetRuleNames()
    {
        var transition = DomainException.InvalidStateTransition<SampleEntity>("Pending", "Shipped");
        var rule = DomainException.RuleViolation<SampleEntity>("Amount must be positive");

        transition.RuleName.Should().Be("INVALID_STATE_TRANSITION");
        rule.EntityName.Should().Be(nameof(SampleEntity));
    }

    [Fact]
    public void PipelineException_StoresOperationName()
    {
        var exception = new PipelineException("failed", "PIPELINE_ERROR", "ValidateStep");

        exception.OperationName.Should().Be("ValidateStep");
        exception.ErrorCode.Should().Be("PIPELINE_ERROR");
    }

    [Fact]
    public void BusinessException_StoresInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new BusinessException("rule violated", "BUSINESS", inner);

        exception.InnerException.Should().Be(inner);
    }

    [Fact]
    public void DataException_And_ConfigurationException_StoreErrorCode()
    {
        var data = new DataException("db failed", "DATA_ERROR");
        var config = new ConfigurationException("missing key", "CONFIG_ERROR");

        data.ErrorCode.Should().Be("DATA_ERROR");
        config.ErrorCode.Should().Be("CONFIG_ERROR");
    }

    [Fact]
    public void HttpStatusCodeException_StoresRequestMetadata()
    {
        var uri = new Uri("https://example.com/api");
        var exception = new HttpStatusCodeException("failed", HttpStatusCode.BadGateway, HttpMethod.Get, uri, "{}");

        exception.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        exception.Method.Should().Be(HttpMethod.Get);
        exception.RequestUri.Should().Be(uri);
        exception.ResponseBody.Should().Be("{}");
    }

    private sealed class SampleEntity;
}
