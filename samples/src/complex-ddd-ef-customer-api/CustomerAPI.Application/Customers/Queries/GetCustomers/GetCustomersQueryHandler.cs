using AutoMapper;
using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Resources;
using CustomerAPI.Core.Specifications.Customers;
using CustomerAPI.Core.ValueObjects.Customers;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace CustomerAPI.Application.Customers.Queries.GetCustomers;

public sealed class GetCustomersQueryHandler(
    IUnitOfWorkAsync unitOfWork,
    IMapper mapper)
    : IMediatorQueryHandler<GetCustomersQuery, IPagingResult<IList<CustomerResult>>>
{
    public async Task<IPagingResult<IList<CustomerResult>>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var filter = request.Filter;

        Expression<Func<Customer, bool>> clause =
            x => (string.IsNullOrEmpty(filter.Name) || x.Name.Contains(filter.Name))
                && (filter.Active == null || x.Active == filter.Active.Value);

        if (filter.HasCellContact)
            clause = clause.And<Customer, CustomerHasCellContactSpec>();

        if (filter.HasEmailContact)
            clause = clause.And<Customer, CustomerHasEmailContactSpec>();

        if (filter.HasNoContact)
            clause = clause.And<Customer, CustomerHasNoContactSpec>();

        if (filter.IsProspect)
            clause = clause.And<Customer, CustomerIsProspectSpec>();

        var repository = unitOfWork.GetRepository<Customer>();
        var result = await repository.ToBusinessPagingAsync(clause, request.Criteria);

        if (!result.HasData())
        {
            return Messages.RECORD_NOT_FOUND
                .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND), MessageType.Error)
                .ToBusinessPaging<IList<CustomerResult>>();
        }

        return mapper.MapPagingTo<IList<Customer>, IList<CustomerResult>>(result);
    }
}
