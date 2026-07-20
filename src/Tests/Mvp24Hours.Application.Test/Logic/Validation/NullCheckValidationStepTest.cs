using System.ComponentModel.DataAnnotations;
using Mvp24Hours.Application.Logic.Validation;

namespace Mvp24Hours.Application.Test.Logic.Validation;

[Trait("Category", "Unit")]
public class NullCheckValidationStepTest
{
    [Fact]
    public void Execute_RequiredNull_ShouldFail()
    {
        var step = new NullCheckValidationStep<RequiredModel>();
        var context = new ValidationStepContext(ValidationOptions.Default, null);

        ValidationServiceResult result = step.Execute(new RequiredModel { Name = null! }, context);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Key == "Name");
    }

    [Fact]
    public void Execute_RequiredEmptyString_WhenAllowEmptyStringsFalse_ShouldFail()
    {
        var step = new NullCheckValidationStep<RequiredModel>();
        var context = new ValidationStepContext(ValidationOptions.Default, null);

        ValidationServiceResult result = step.Execute(new RequiredModel { Name = "   " }, context);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Key == "Name");
    }

    [Fact]
    public void Execute_ValidRequired_ShouldPass()
    {
        var step = new NullCheckValidationStep<RequiredModel>();
        var context = new ValidationStepContext(ValidationOptions.Default, null);

        ValidationServiceResult result = step.Execute(new RequiredModel { Name = "Valid" }, context);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Execute_StopOnFirstError_ShouldReturnAfterFirstError()
    {
        var step = new NullCheckValidationStep<MultiRequiredModel>();
        var context = new ValidationStepContext(new ValidationOptions { StopOnFirstError = true }, null);

        ValidationServiceResult result = step.Execute(new MultiRequiredModel(), context);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMatchSyncResult()
    {
        var step = new NullCheckValidationStep<RequiredModel>();
        var context = new ValidationStepContext(ValidationOptions.Default, null);
        var instance = new RequiredModel { Name = "OK" };

        ValidationServiceResult sync = step.Execute(instance, context);
        ValidationServiceResult async = await step.ExecuteAsync(instance, context);

        async.IsValid.Should().Be(sync.IsValid);
        async.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ShouldExecute_ShouldAlwaysBeTrue()
    {
        var step = new NullCheckValidationStep<RequiredModel>();
        var context = new ValidationStepContext(ValidationOptions.Default, null);

        step.ShouldExecute(new RequiredModel(), context).Should().BeTrue();
    }

    [Fact]
    public void Execute_IncludePropertyPath_ShouldPrefixPath()
    {
        var step = new NullCheckValidationStep<RequiredModel>();
        var context = new ValidationStepContext(
            new ValidationOptions { IncludePropertyPath = true },
            null)
        {
            PropertyPath = "Parent"
        };

        ValidationServiceResult result = step.Execute(new RequiredModel { Name = null! }, context);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Key == "Parent.Name");
    }

    [Fact]
    public void Execute_AllowEmptyStringsTrue_ShouldPassForEmptyString()
    {
        var step = new NullCheckValidationStep<OptionalEmptyModel>();
        var context = new ValidationStepContext(ValidationOptions.Default, null);

        ValidationServiceResult result = step.Execute(new OptionalEmptyModel { Code = "" }, context);

        result.IsValid.Should().BeTrue();
    }

    private sealed class RequiredModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;
    }

    private sealed class MultiRequiredModel
    {
        [Required]
        public string First { get; set; } = string.Empty;

        [Required]
        public string Second { get; set; } = string.Empty;
    }

    private sealed class OptionalEmptyModel
    {
        [Required(AllowEmptyStrings = true)]
        public string Code { get; set; } = string.Empty;
    }
}
