using CustomerAPI.Application.DTOs.Customers;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Customers.Queries.GetCustomers;

public sealed class GetCustomersQuery : IMediatorQuery<IBusinessResult<IList<CustomerResult>>>
{
    public string? Name { get; init; }
    public bool? Active { get; init; }
}
