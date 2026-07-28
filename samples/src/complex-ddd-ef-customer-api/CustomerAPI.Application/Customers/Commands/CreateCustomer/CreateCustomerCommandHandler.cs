using CustomerAPI.Application.Customers.Notifications;
using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Resources;
using CustomerAPI.Core.ValueObjects.Domain;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Customers.Commands.CreateCustomer;

/// <summary>
/// Demonstrates DDD-style aggregate creation:
/// 1. Constructs <see cref="CustomerName"/> value object (validates invariants).
/// 2. Calls the <see cref="Customer.Create"/> factory method (raises domain event on aggregate).
/// 3. Persists via Unit of Work.
/// 4. Dispatches the domain event as an in-process mediator notification.
/// </summary>
public sealed class CreateCustomerCommandHandler(
    IUnitOfWorkAsync unitOfWork,
    TimeProvider timeProvider,
    IMediator mediator,
    ILogger<CreateCustomerCommandHandler> logger)
    : IMediatorCommandHandler<CreateCustomerCommand, IBusinessResult<int>>
{
    public async Task<IBusinessResult<int>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        CustomerName name;
        try
        {
            name = new CustomerName(request.Model.Name);
        }
        catch (ArgumentException ex)
        {
            return ex.Message
                .ToMessageResult("DOMAIN_VALIDATION", MessageType.Error)
                .ToBusiness<int>();
        }

        var customer = Customer.Create(name, timeProvider, request.Model.Note);

        var repository = unitOfWork.GetRepository<Customer>();
        await repository.AddAsync(customer, cancellationToken: cancellationToken);

        if (await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken) <= 0)
        {
            return Messages.OPERATION_FAIL
                .ToMessageResult(nameof(Messages.OPERATION_FAIL), MessageType.Error)
                .ToBusiness<int>();
        }

        logger.LogInformation("Created customer {CustomerId} ({CustomerName})", customer.Id, customer.Name);

        await mediator.PublishAsync(
            new CustomerCreatedNotification(customer.Id, customer.Name),
            cancellationToken);

        customer.ClearDomainEvents();

        return customer.Id.ToBusiness(
            Messages.OPERATION_SUCCESS
                .ToMessageResult(nameof(Messages.OPERATION_SUCCESS), MessageType.Success));
    }
}
