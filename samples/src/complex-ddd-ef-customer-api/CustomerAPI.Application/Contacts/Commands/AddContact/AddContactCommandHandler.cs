using CustomerAPI.Application.Contacts.Notifications;
using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Exceptions;
using CustomerAPI.Core.Resources;
using CustomerAPI.Core.ValueObjects.Domain;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Contacts.Commands.AddContact;

/// <summary>
/// Demonstrates aggregate-driven contact creation:
/// 1. Loads the Customer aggregate (with its Contacts for state completeness).
/// 2. Constructs <see cref="ContactDescription"/> value object.
/// 3. Calls <see cref="Customer.AddContact"/> — domain enforces invariant (active customer only).
/// 4. Persists; EF resolves the FK automatically.
/// 5. Dispatches domain event as in-process notification.
/// </summary>
public sealed class AddContactCommandHandler(
    IUnitOfWorkAsync unitOfWork,
    TimeProvider timeProvider,
    IMediator mediator,
    ILogger<AddContactCommandHandler> logger)
    : IMediatorCommandHandler<AddContactCommand, IBusinessResult<int>>
{
    public async Task<IBusinessResult<int>> Handle(AddContactCommand request, CancellationToken cancellationToken)
    {
        // Load aggregate with contacts so domain can evaluate full state.
        var paging = new PagingCriteriaExpression<Customer>(100, 0);
        paging.NavigationExpr.Add(x => x.Contacts);

        var repository = unitOfWork.GetRepository<Customer>();
        var customer = await repository.GetByIdAsync(request.CustomerId, paging, cancellationToken);

        if (customer is null)
        {
            return Messages.RECORD_NOT_FOUND_FOR_ID
                .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND_FOR_ID), MessageType.Error)
                .ToBusiness<int>();
        }

        ContactDescription description;
        try
        {
            description = new ContactDescription(request.Model.Description);
        }
        catch (ArgumentException ex)
        {
            return ex.Message
                .ToMessageResult("DOMAIN_VALIDATION", MessageType.Error)
                .ToBusiness<int>();
        }

        Contact newContact;
        try
        {
            newContact = customer.AddContact(request.Model.Type, description, timeProvider);
        }
        catch (DomainException ex)
        {
            return ex.Message
                .ToMessageResult("DOMAIN_RULE", MessageType.Error)
                .ToBusiness<int>();
        }

        if (await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken) <= 0)
        {
            return Messages.OPERATION_FAIL
                .ToMessageResult(nameof(Messages.OPERATION_FAIL), MessageType.Error)
                .ToBusiness<int>();
        }

        logger.LogInformation("Added contact {ContactId} to customer {CustomerId}", newContact.Id, customer.Id);

        await mediator.PublishAsync(
            new ContactAddedNotification(customer.Id, customer.Name, request.Model.Type, description.Value),
            cancellationToken);

        customer.ClearDomainEvents();

        return newContact.Id.ToBusiness(
            Messages.OPERATION_SUCCESS
                .ToMessageResult(nameof(Messages.OPERATION_SUCCESS), MessageType.Success));
    }
}
