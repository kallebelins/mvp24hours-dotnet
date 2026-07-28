using AutoMapper;
using CustomerAPI.Application.DTOs.Contacts;
using CustomerAPI.Domain.Entities;
using CustomerAPI.Domain.Resources;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using System.Collections.Generic;

namespace CustomerAPI.Application.Contacts.Queries.GetContactsByCustomer;

public sealed class GetContactsByCustomerQueryHandler(
    IUnitOfWorkAsync unitOfWork,
    IMapper mapper)
    : IMediatorQueryHandler<GetContactsByCustomerQuery, IBusinessResult<IList<ContactIdResult>>>
{
    public async Task<IBusinessResult<IList<ContactIdResult>>> Handle(
        GetContactsByCustomerQuery request,
        CancellationToken cancellationToken)
    {
        var repository = unitOfWork.GetRepository<Contact>();
        var result = await repository
            .GetByAsync(x => x.CustomerId == request.CustomerId, cancellationToken: cancellationToken)
            .ToBusinessAsync();

        if (!result.HasData())
        {
            return Messages.RECORD_NOT_FOUND
                .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND), MessageType.Error)
                .ToBusiness<IList<ContactIdResult>>();
        }

        return mapper.MapBusinessTo<IList<Contact>, IList<ContactIdResult>>(result);
    }
}
