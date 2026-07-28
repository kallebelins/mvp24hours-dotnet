using CustomerAPI.Application.DTOs.Customers;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Customers.Commands.CreateCustomer;

public sealed class CreateCustomerCommand : IMediatorCommand<IBusinessResult<CustomerIdResult>>
{
    public CreateCustomerCommand(CustomerCreate model, string? correlationId = null)
    {
        Model = model;
        CorrelationId = correlationId;
    }

    public CustomerCreate Model { get; }

    /// <summary>Propagated from HTTP headers (X-Correlation-Id) for distributed tracing.</summary>
    public string? CorrelationId { get; }
}
