using Mvp24Hours.Application.Pipe.Test.Operations;
using Mvp24Hours.Application.Pipe.Test.Support;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Validation;

namespace Mvp24Hours.Application.Pipe.Test.Validation;

[Trait("Category", "Unit")]
public class DefaultPipelineValidatorTest
{
    [Fact]
    public void Validate_Should_SucceedForValidOperations()
    {
        var validator = new DefaultPipelineValidator();
        object[] operations = [new OperationTest(), new TrackingOperation("valid")];

        PipelineValidationResult result = validator.Validate(operations);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_FailWhenNoOperationsRequired()
    {
        var validator = new DefaultPipelineValidator().RequireAtLeastOneOperation();

        PipelineValidationResult result = validator.Validate([]);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "NO_OPERATIONS");
    }

    [Fact]
    public void Validate_Should_FailWhenTooManyOperations()
    {
        var validator = new DefaultPipelineValidator().WithMaxOperations(1);
        object[] operations = [new OperationTest(), new TrackingOperation("extra")];

        PipelineValidationResult result = validator.Validate(operations);

        result.Errors.Should().Contain(e => e.Code == "TOO_MANY_OPERATIONS");
    }

    [Fact]
    public void Validate_Should_DetectDuplicateOperationInstances()
    {
        var shared = new OperationTest();
        var validator = new DefaultPipelineValidator();

        PipelineValidationResult result = validator.Validate([shared, shared]);

        result.Errors.Should().Contain(e => e.Code == "DUPLICATE_OPERATION_INSTANCE");
    }

    [Fact]
    public void Validate_Should_EnforceRequiredOperationType()
    {
        var validator = new DefaultPipelineValidator().RequireOperation<OperationTest>();

        PipelineValidationResult missing = validator.Validate([new TrackingOperation("only-tracking")]);
        PipelineValidationResult valid = validator.Validate([new OperationTest()]);

        missing.Errors.Should().Contain(e => e.Code == "MISSING_REQUIRED_OPERATION");
        valid.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_RunCustomRules()
    {
        var validator = new DefaultPipelineValidator()
            .AddRule(_ => [new PipelineValidationError("CUSTOM", "custom validation failed")]);

        PipelineValidationResult result = validator.Validate([new OperationTest()]);

        result.Errors.Should().Contain(e => e.Code == "CUSTOM");
    }

    [Fact]
    public void Validate_Should_DetectNullOperation()
    {
        var validator = new DefaultPipelineValidator();

        PipelineValidationResult result = validator.Validate([new OperationTest(), null!]);

        result.Errors.Should().Contain(e => e.Code == "NULL_OPERATION");
    }
}
