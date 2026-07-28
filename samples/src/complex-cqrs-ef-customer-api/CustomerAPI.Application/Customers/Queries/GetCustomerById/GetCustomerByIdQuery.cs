using CustomerAPI.Core.ValueObjects.Customers;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Customers.Queries.GetCustomerById;

public sealed class GetCustomerByIdQuery : IMediatorQuery<IBusinessResult<CustomerIdResult>>
{
    public int Id { get; init; }
}
