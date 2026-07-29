using CustomerAPI.Core.ValueObjects.Customers;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Customers.Queries.GetCustomers;

public sealed class GetCustomersQuery : IMediatorQuery<IPagingResult<IList<CustomerResult>>>
{
    public required CustomerQuery Filter { get; init; }
    public required IPagingCriteria Criteria { get; init; }
}
