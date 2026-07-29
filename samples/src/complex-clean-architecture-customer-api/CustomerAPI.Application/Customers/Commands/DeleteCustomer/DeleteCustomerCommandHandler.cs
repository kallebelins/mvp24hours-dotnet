using CustomerAPI.Domain.Entities;
using CustomerAPI.Domain.Resources;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Customers.Commands.DeleteCustomer;

public sealed class DeleteCustomerCommandHandler(
    IUnitOfWorkAsync unitOfWork,
    ILogger<DeleteCustomerCommandHandler> logger)
    : IMediatorCommandHandler<DeleteCustomerCommand, IBusinessResult<int>>
{
    public async Task<IBusinessResult<int>> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        IRepositoryAsync<Customer> repository = unitOfWork.GetRepository<Customer>();
        Customer? entity = await repository.GetByIdAsync(request.Id, cancellationToken: cancellationToken);
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
            logger.LogInformation("Deleted customer {CustomerId}", request.Id);
            return affectedRows.ToBusiness(
                Messages.OPERATION_SUCCESS
                    .ToMessageResult(nameof(Messages.OPERATION_SUCCESS), MessageType.Success));
        }

        return Messages.OPERATION_FAIL
            .ToMessageResult(nameof(Messages.OPERATION_FAIL), MessageType.Error)
            .ToBusiness<int>();
    }
}
