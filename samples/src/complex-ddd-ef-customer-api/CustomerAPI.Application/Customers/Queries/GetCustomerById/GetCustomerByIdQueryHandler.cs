using AutoMapper;
using CustomerAPI.Core.Entities;
using CustomerAPI.Core.ValueObjects.Customers;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Customers.Queries.GetCustomerById;

public sealed class GetCustomerByIdQueryHandler(
    IUnitOfWorkAsync unitOfWork,
    IMapper mapper)
    : IMediatorQueryHandler<GetCustomerByIdQuery, IBusinessResult<CustomerIdResult>>
{
    public async Task<IBusinessResult<CustomerIdResult>> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var paging = new PagingCriteriaExpression<Customer>(3, 0);
        paging.NavigationExpr.Add(x => x.Contacts);

        var repository = unitOfWork.GetRepository<Customer>();
        return await mapper.MapBusinessToAsync<Customer, CustomerIdResult>(
            repository.GetByIdAsync(request.Id, paging, cancellationToken).ToBusinessAsync());
    }
}
