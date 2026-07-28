using CustomerAPI.Domain.Sagas;
using Mvp24Hours.Infrastructure.Cqrs.Saga;

namespace CustomerAPI.Application.Sagas.Steps;

/// <summary>
/// Step 2 — Simulates an external gift-service reservation call.
/// Pass <c>SimulateGiftFailure = true</c> in the request to trigger a
/// failure and watch the saga compensate Step 1.
/// </summary>
public class ReserveWelcomeGiftStep : SagaStepBase<OnboardCustomerData>
{
    public override string Name => "ReserveWelcomeGift";
    public override int Order => 2;

    public override Task ExecuteAsync(OnboardCustomerData data, CancellationToken cancellationToken = default)
    {
        if (data.SimulateGiftFailure)
        {
            throw new InvalidOperationException(
                "Gift service is unavailable — compensation will be triggered.");
        }

        // Simulate generating a gift code (would normally call an external API)
        data.WelcomeGiftCode = $"WELCOME-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
        return Task.CompletedTask;
    }

    public override Task CompensateAsync(OnboardCustomerData data, CancellationToken cancellationToken = default)
    {
        // In a real system we would call the gift service to release/invalidate the reservation
        data.WelcomeGiftCode = null;
        return Task.CompletedTask;
    }
}
