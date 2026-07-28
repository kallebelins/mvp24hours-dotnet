using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Customers.Commands.DeactivateCustomer;

public sealed class DeactivateCustomerCommand : IMediatorCommand<IBusinessResult<int>>
{
    public int Id { get; init; }
}
