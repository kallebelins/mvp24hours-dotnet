using AutoMapper;
using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Resources;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Contacts.Commands.UpdateContact;

public sealed class UpdateContactCommandHandler(
    IUnitOfWorkAsync unitOfWork,
    IMapper mapper,
    ILogger<UpdateContactCommandHandler> logger)
    : IMediatorCommandHandler<UpdateContactCommand, IBusinessResult<int>>
{
    public async Task<IBusinessResult<int>> Handle(UpdateContactCommand request, CancellationToken cancellationToken)
    {
        IRepositoryAsync<Contact> repository = unitOfWork.GetRepository<Contact>();
        Contact? entity = await repository
            .GetByAsync(x => x.Id == request.Id && x.CustomerId == request.CustomerId, cancellationToken: cancellationToken)
            .FirstOrDefaultAsync();

        if (entity == null)
        {
            return Messages.RECORD_NOT_FOUND_FOR_ID
                .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND_FOR_ID), MessageType.Error)
                .ToBusiness<int>();
        }

        mapper.Map(request.Model, entity);
        await repository.ModifyAsync(entity, cancellationToken: cancellationToken);

        int affectedRows = await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
        if (affectedRows > 0)
        {
            logger.LogInformation("Updated contact {ContactId} for customer {CustomerId}", request.Id, request.CustomerId);
            return affectedRows.ToBusiness(
                Messages.OPERATION_SUCCESS
                    .ToMessageResult(nameof(Messages.OPERATION_SUCCESS), MessageType.Success));
        }

        return Messages.OPERATION_FAIL
            .ToMessageResult(nameof(Messages.OPERATION_FAIL), MessageType.Error)
            .ToBusiness<int>();
    }
}
