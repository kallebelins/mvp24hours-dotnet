using CustomerAPI.Domain.Sagas;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Cqrs.Saga;

namespace CustomerAPI.Application.Sagas.Steps;

/// <summary>
/// Step 3 — Simulates sending a welcome e-mail.
/// CanCompensate = false: an e-mail that was delivered cannot be "unsent";
/// the saga will still compensate the preceding steps but skip this one.
/// </summary>
public class SendWelcomeEmailStep(ILogger<SendWelcomeEmailStep> logger) : SagaStepBase<OnboardCustomerData>
{
    public override string Name => "SendWelcomeEmail";
    public override int Order => 3;
    public override bool CanCompensate => false;

    public override Task ExecuteAsync(OnboardCustomerData data, CancellationToken cancellationToken = default)
    {
        // In a real system, call your e-mail provider SDK here.
        logger.LogInformation(
            "Welcome e-mail sent to {Email} (gift code: {Code})",
            data.Email, data.WelcomeGiftCode ?? "none");

        data.WelcomeEmailSent = true;
        return Task.CompletedTask;
    }
}
