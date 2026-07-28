using CustomerAPI.Application.DTOs.Contacts;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Contacts.Commands.CreateContact;

public sealed class CreateContactCommand : IMediatorCommand<IBusinessResult<int>>
{
    public int CustomerId { get; init; }
    public required ContactCreate Model { get; init; }
}
