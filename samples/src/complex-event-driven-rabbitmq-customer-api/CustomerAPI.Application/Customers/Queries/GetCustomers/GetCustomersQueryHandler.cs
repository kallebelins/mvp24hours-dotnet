using AutoMapper;
using CustomerAPI.Application.DTOs.Customers;
using CustomerAPI.Domain.Entities;
using CustomerAPI.Domain.Specifications.Customers;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using System;
using System.Linq.Expressions;

namespace CustomerAPI.Application.Customers.Queries.GetCustomers;

public sealed class GetCustomersQueryHandler(
    IUnitOfWorkAsync unitOfWork,
    IMapper mapper)
    : IMediatorQueryHandler<GetCustomersQuery, IBusinessResult<IList<CustomerResult>>>
{
    public async Task<IBusinessResult<IList<CustomerResult>>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.GetRepository<Customer>();

        Expression<Func<Customer, bool>> clause =
            x => request.Name == null || x.Name.Contains(request.Name);

        if (request.Active == true)
        {
            clause = clause.And<Customer, CustomerIsActiveSpec>();
        }
        else if (request.Active == false)
        {
            clause = clause.And(x => !x.Active);
        }

        var customers = await repository.GetByAsync(clause, cancellationToken: cancellationToken);

        if (customers == null || !customers.Any())
        {
            return "RECORD_NOT_FOUND"
                .ToMessageResult("RECORD_NOT_FOUND", MessageType.Error)
                .ToBusiness<IList<CustomerResult>>();
        }

        var result = mapper.Map<IList<CustomerResult>>(customers);

        return result.ToBusiness(
            "OPERATION_SUCCESS".ToMessageResult("OPERATION_SUCCESS", MessageType.Success));
    }
}
