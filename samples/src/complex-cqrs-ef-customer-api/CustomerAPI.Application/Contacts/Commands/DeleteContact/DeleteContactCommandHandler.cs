using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Resources;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Contacts.Commands.DeleteContact;

public sealed class DeleteContactCommandHandler(
    IUnitOfWorkAsync unitOfWork,
    ILogger<DeleteContactCommandHandler> logger)
    : IMediatorCommandHandler<DeleteContactCommand, IBusinessResult<int>>
{
    public async Task<IBusinessResult<int>> Handle(DeleteContactCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.GetRepository<Contact>();
        var entity = await repository
            .GetByAsync(x => x.Id == request.Id && x.CustomerId == request.CustomerId, cancellationToken: cancellationToken)
            .FirstOrDefaultAsync();

        if (entity == null)
        {
            return Messages.RECORD_NOT_FOUND_FOR_ID
                .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND_FOR_ID), MessageType.Error)
                .ToBusiness<int>();
        }

        await repository.RemoveAsync(entity, cancellationToken: cancellationToken);
        int affectedRows = await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
        if (affectedRows > 0)
        {
            logger.LogInformation("Deleted contact {ContactId} for customer {CustomerId}", request.Id, request.CustomerId);
            return affectedRows.ToBusiness(
                Messages.OPERATION_SUCCESS
                    .ToMessageResult(nameof(Messages.OPERATION_SUCCESS), MessageType.Success));
        }

        return Messages.OPERATION_FAIL
            .ToMessageResult(nameof(Messages.OPERATION_FAIL), MessageType.Error)
            .ToBusiness<int>();
    }
}
