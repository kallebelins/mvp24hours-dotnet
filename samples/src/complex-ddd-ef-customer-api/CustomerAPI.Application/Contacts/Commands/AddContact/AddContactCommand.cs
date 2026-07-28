using CustomerAPI.Core.ValueObjects.Contacts;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Contacts.Commands.AddContact;

public sealed class AddContactCommand : IMediatorCommand<IBusinessResult<int>>
{
    public int CustomerId { get; init; }
    public required ContactCreate Model { get; init; }
}
