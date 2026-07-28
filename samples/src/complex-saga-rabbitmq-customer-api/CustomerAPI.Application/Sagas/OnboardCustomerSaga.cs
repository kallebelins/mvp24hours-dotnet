using CustomerAPI.Application.Sagas.Steps;
using CustomerAPI.Domain.Sagas;
using Mvp24Hours.Infrastructure.Cqrs.Saga;

namespace CustomerAPI.Application.Sagas;

/// <summary>
/// Orchestrates the multi-step customer onboarding process.
///
/// Steps (in order):
///   1. CreateCustomerStep    — persist customer record
///   2. ReserveWelcomeGiftStep — external gift-service reservation (can fail)
///   3. SendWelcomeEmailStep  — fire-and-forget e-mail (not compensable)
///
/// If any step fails the saga automatically compensates all already-executed
/// steps in reverse order (LIFO), demonstrating the Saga Compensation pattern.
/// </summary>
public class OnboardCustomerSaga : SagaBase<OnboardCustomerData>
{
    public OnboardCustomerSaga(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        ConfigureSteps(steps =>
        {
            steps.Add<CreateCustomerStep>();
            steps.Add<ReserveWelcomeGiftStep>();
            steps.Add<SendWelcomeEmailStep>();
        });

        WithTimeout(TimeSpan.FromSeconds(30));
        // Disable automatic retries — the gift service failure should trigger compensation immediately
        WithMaxRetries(0);
    }
}
