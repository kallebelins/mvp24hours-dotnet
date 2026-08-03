using Mvp24Hours.Application.Logic.Validation;

namespace Mvp24Hours.Application.Test.Logic.Validation;

[Trait("Category", "Unit")]
public class CustomValidationStepTest
{
    [Fact]
    public void RuleBasedValidationStep_InvalidRule_ShouldReturnFailure()
    {
        RuleBasedValidationStep<RuleModel> step = new RuleBasedValidationStep<RuleModel>("BusinessRules")
            .AddRule("Amount", m => m.Amount > 0, "Amount must be positive");

        ValidationServiceResult result = step.Execute(new RuleModel { Amount = -1 }, new ValidationStepContext(ValidationOptions.Default, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Key == "Amount");
    }

    [Fact]
    public async Task RuleBasedValidationStep_AsyncRule_ShouldValidateAsynchronously()
    {
        RuleBasedValidationStep<RuleModel> step = new RuleBasedValidationStep<RuleModel>("AsyncRules")
            .AddRuleAsync("Amount", async (_, ct) =>
            {
                await Task.Delay(1, ct);
                return false;
            }, "Async validation failed");

        ValidationServiceResult result = await step.ExecuteAsync(
            new RuleModel { Amount = 10 },
            new ValidationStepContext(ValidationOptions.Default, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void PredicateValidationStep_ShouldExecuteOnlyWhenShouldExecuteReturnsTrue()
    {
        int calls = 0;
        var step = new PredicateValidationStep<RuleModel>(
            "Conditional",
            (_, _) =>
            {
                calls++;
                return ValidationServiceResult.Success();
            },
            shouldExecuteFunc: (_, _) => false);
        var pipeline = new ValidationPipeline<RuleModel>([step]);

        pipeline.Execute(new RuleModel(), ValidationOptions.Default);

        calls.Should().Be(0);
    }

    [Fact]
    public void CustomStep_CreateError_ShouldRespectPropertyPath()
    {
        var step = new NameRequiredStep();
        var context = new ValidationStepContext(new ValidationOptions { IncludePropertyPath = true }, null)
        {
            PropertyPath = "Root"
        };

        ValidationServiceResult result = step.Execute(new RuleModel { Name = "" }, context);

        result.Errors.Should().ContainSingle(e => e.Key == "Root.Name");
    }

    private sealed class RuleModel
    {
        public decimal Amount { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class NameRequiredStep : CustomValidationStep<RuleModel>
    {
        public override string Name => "NameRequired";

        public override ValidationServiceResult Execute(RuleModel instance, ValidationStepContext context)
        {
            if (string.IsNullOrWhiteSpace(instance.Name))
            {
                IMessageResult error = CreateError("Name", "Name is required", context);
                return ValidationServiceResult.Failure([error]);
            }

            return ValidationServiceResult.Success();
        }
    }
}
