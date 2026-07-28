using CustomerAPI.Core.ValueObjects.Customers;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Customers.Commands.UpdateCustomer;

public sealed class UpdateCustomerCommand : IMediatorCommand<IBusinessResult<int>>
{
    public int Id { get; init; }
    public required CustomerUpdate Model { get; init; }
}
