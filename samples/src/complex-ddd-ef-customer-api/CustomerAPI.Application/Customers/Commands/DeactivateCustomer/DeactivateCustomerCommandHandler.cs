using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Resources;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Customers.Commands.DeactivateCustomer;

/// <summary>
/// Demonstrates a domain-specific command: <c>Deactivate</c> is a named intent,
/// not a generic "update Active = false".
/// </summary>
public sealed class DeactivateCustomerCommandHandler(
    IUnitOfWorkAsync unitOfWork,
    ILogger<DeactivateCustomerCommandHandler> logger)
    : IMediatorCommandHandler<DeactivateCustomerCommand, IBusinessResult<int>>
{
    public async Task<IBusinessResult<int>> Handle(DeactivateCustomerCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.GetRepository<Customer>();
        var customer = await repository.GetByIdAsync(request.Id, cancellationToken: cancellationToken);

        if (customer is null)
        {
            return Messages.RECORD_NOT_FOUND_FOR_ID
                .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND_FOR_ID), MessageType.Error)
                .ToBusiness<int>();
        }

        customer.Deactivate();

        await repository.ModifyAsync(customer, cancellationToken: cancellationToken);
        int rows = await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        logger.LogInformation("Deactivated customer {CustomerId}", customer.Id);

        return rows.ToBusiness(
            Messages.OPERATION_SUCCESS
                .ToMessageResult(nameof(Messages.OPERATION_SUCCESS), MessageType.Success));
    }
}
