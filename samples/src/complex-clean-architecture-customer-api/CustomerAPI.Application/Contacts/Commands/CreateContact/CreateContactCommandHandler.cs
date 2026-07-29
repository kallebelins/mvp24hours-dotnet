using AutoMapper;
using CustomerAPI.Domain.Entities;
using CustomerAPI.Domain.Resources;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Contacts.Commands.CreateContact;

public sealed class CreateContactCommandHandler(
    IUnitOfWorkAsync unitOfWork,
    IMapper mapper,
    TimeProvider timeProvider,
    ILogger<CreateContactCommandHandler> logger)
    : IMediatorCommandHandler<CreateContactCommand, IBusinessResult<int>>
{
    public async Task<IBusinessResult<int>> Handle(CreateContactCommand request, CancellationToken cancellationToken)
    {
        Contact entity = mapper.Map<Contact>(request.Model);
        entity.CustomerId = request.CustomerId;
        entity.Created = timeProvider.GetUtcNow().UtcDateTime;
        entity.Active = true;

        IRepositoryAsync<Contact> repository = unitOfWork.GetRepository<Contact>();
        await repository.AddAsync(entity, cancellationToken: cancellationToken);

        if (await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken) <= 0)
        {
            return Messages.OPERATION_FAIL
                .ToMessageResult(nameof(Messages.OPERATION_FAIL), MessageType.Error)
                .ToBusiness<int>();
        }

        logger.LogInformation("Created contact {ContactId} for customer {CustomerId}", entity.Id, request.CustomerId);
        return entity.Id.ToBusiness(
            Messages.OPERATION_SUCCESS
                .ToMessageResult(nameof(Messages.OPERATION_SUCCESS), MessageType.Success));
    }
}
