using CustomerAPI.Core.ValueObjects.Customers;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Customers.Commands.CreateCustomer;

public sealed class CreateCustomerCommand : IMediatorCommand<IBusinessResult<int>>
{
    public required CustomerCreate Model { get; init; }
}
