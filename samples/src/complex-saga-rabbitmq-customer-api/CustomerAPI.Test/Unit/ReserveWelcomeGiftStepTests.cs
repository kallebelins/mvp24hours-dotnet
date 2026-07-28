using CustomerAPI.Application.Sagas.Steps;
using CustomerAPI.Domain.Sagas;

namespace CustomerAPI.Test.Unit;

[Trait("Category", "Unit")]
public class ReserveWelcomeGiftStepTests
{
    [Fact]
    public async Task ReserveWelcomeGiftStep_WhenSimulateFailure_Throws()
    {
        var step = new ReserveWelcomeGiftStep();
        var data = new OnboardCustomerData
        {
            Name = "Ada",
            Email = "ada@example.com",
            SimulateGiftFailure = true
        };

        Func<Task> act = () => step.ExecuteAsync(data);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Gift service is unavailable*");
        data.WelcomeGiftCode.Should().BeNull();
    }

    [Fact]
    public async Task ReserveWelcomeGiftStep_WhenSuccess_SetsGiftCode()
    {
        var step = new ReserveWelcomeGiftStep();
        var data = new OnboardCustomerData
        {
            Name = "Ada",
            Email = "ada@example.com",
            SimulateGiftFailure = false
        };

        await step.ExecuteAsync(data);

        data.WelcomeGiftCode.Should().NotBeNullOrWhiteSpace();
        data.WelcomeGiftCode.Should().StartWith("WELCOME-");
        data.WelcomeGiftCode!.Length.Should().Be(16);
    }
}
