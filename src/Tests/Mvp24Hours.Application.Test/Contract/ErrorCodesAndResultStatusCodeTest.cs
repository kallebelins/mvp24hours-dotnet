using System.Reflection;
using Mvp24Hours.Application.Contract.Events;
using Mvp24Hours.Application.Contract.Resilience;
using Mvp24Hours.Application.Test.Support;

namespace Mvp24Hours.Application.Test.Contract;

[Trait("Category", "Unit")]
public class ErrorCodesAndResultStatusCodeTest
{
    [Theory]
    [InlineData(typeof(ErrorCodes.Validation))]
    [InlineData(typeof(ErrorCodes.Auth))]
    [InlineData(typeof(ErrorCodes.Resource))]
    [InlineData(typeof(ErrorCodes.Domain))]
    [InlineData(typeof(ErrorCodes.Operation))]
    [InlineData(typeof(ErrorCodes.System))]
    public void ErrorCodes_ConstantsWithinCategory_ShouldBeNonEmptyAndUnique(Type nestedType)
    {
        string[] codes = [.. nestedType
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)];

        codes.Should().NotBeEmpty();
        codes.Should().OnlyContain(code => !string.IsNullOrWhiteSpace(code));
        codes.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void ErrorCodes_SampleKeys_ShouldHaveExpectedValues()
    {
        ErrorCodes.Validation.Failed.Should().Be("VALIDATION.FAILED");
        ErrorCodes.Auth.Unauthorized.Should().Be("AUTH.UNAUTHORIZED");
        ErrorCodes.Resource.NotFound.Should().Be("RESOURCE.NOT_FOUND");
        ErrorCodes.Domain.RuleViolation.Should().Be("DOMAIN.RULE_VIOLATION");
        ErrorCodes.System.InternalError.Should().Be("SYSTEM.INTERNAL_ERROR");
    }

    [Theory]
    [InlineData(ResultStatusCode.Success, 0)]
    [InlineData(ResultStatusCode.ValidationFailed, 100)]
    [InlineData(ResultStatusCode.Unauthorized, 200)]
    [InlineData(ResultStatusCode.NotFound, 300)]
    [InlineData(ResultStatusCode.DomainRuleViolation, 400)]
    [InlineData(ResultStatusCode.InternalError, 500)]
    public void ResultStatusCode_KeyValues_ShouldBeInExpectedRanges(ResultStatusCode code, int expected)
    {
        ((int)code).Should().Be(expected);
    }

    [Fact]
    public void EntityCreatedEvent_ShouldExposeEntityAndOperationType()
    {
        var entity = new AppTestEntity { Id = 1, Name = "Created" };

        var @event = new EntityCreatedEvent<AppTestEntity>(entity);

        @event.Entity.Should().BeSameAs(entity);
        @event.OperationType.Should().Be(EntityOperationType.Created);
        @event.EventId.Should().NotBeEmpty();
    }

    [Fact]
    public void EntityUpdatedEvent_WithOriginalEntity_ShouldExposeBothStates()
    {
        var current = new AppTestEntity { Id = 1, Name = "Updated" };
        var original = new AppTestEntity { Id = 1, Name = "Original" };

        var @event = new EntityUpdatedEvent<AppTestEntity>(current, original);

        @event.Entity.Should().BeSameAs(current);
        @event.OriginalEntity.Should().BeSameAs(original);
        @event.OperationType.Should().Be(EntityOperationType.Updated);
    }

    [Fact]
    public void EntityDeletedEvent_NullEntity_ShouldThrow()
    {
        Action act = () => new EntityDeletedEvent<AppTestEntity>(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("entity");
    }
}
