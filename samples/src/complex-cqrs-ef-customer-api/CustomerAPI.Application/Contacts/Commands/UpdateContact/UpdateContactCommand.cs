using CustomerAPI.Core.ValueObjects.Contacts;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Contacts.Commands.UpdateContact;

public sealed class UpdateContactCommand : IMediatorCommand<IBusinessResult<int>>
{
    public int CustomerId { get; init; }
    public int Id { get; init; }
    public required ContactUpdate Model { get; init; }
}
