using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Customers.Commands.DeleteCustomer;

public sealed class DeleteCustomerCommand : IMediatorCommand<IBusinessResult<int>>
{
    public int Id { get; init; }
}
