using CustomerAPI.Application.DTOs.Contacts;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using System.Collections.Generic;

namespace CustomerAPI.Application.Contacts.Queries.GetContactsByCustomer;

public sealed class GetContactsByCustomerQuery : IMediatorQuery<IBusinessResult<IList<ContactIdResult>>>
{
    public int CustomerId { get; init; }
}
