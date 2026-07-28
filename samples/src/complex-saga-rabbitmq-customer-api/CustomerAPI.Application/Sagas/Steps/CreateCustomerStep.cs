using CustomerAPI.Domain.Entities;
using CustomerAPI.Domain.Repositories;
using CustomerAPI.Domain.Sagas;
using Mvp24Hours.Infrastructure.Cqrs.Saga;

namespace CustomerAPI.Application.Sagas.Steps;

/// <summary>
/// Step 1 — Persists the new customer record.
/// Compensation deletes the record if a later step fails.
/// </summary>
public class CreateCustomerStep(ICustomerRepository repository) : SagaStepBase<OnboardCustomerData>
{
    public override string Name => "CreateCustomer";
    public override int Order => 1;

    public override async Task ExecuteAsync(OnboardCustomerData data, CancellationToken cancellationToken = default)
    {
        var customer = new Customer
        {
            Name = data.Name,
            Email = data.Email
        };

        await repository.AddAsync(customer, cancellationToken);
        data.CustomerId = customer.Id;
    }

    public override async Task CompensateAsync(OnboardCustomerData data, CancellationToken cancellationToken = default)
    {
        if (data.CustomerId.HasValue)
        {
            await repository.DeleteAsync(data.CustomerId.Value, cancellationToken);
        }
    }
}
