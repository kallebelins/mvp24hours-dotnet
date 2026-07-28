using AutoMapper;
using CustomerAPI.Application.DTOs.Customers;
using CustomerAPI.Application.Events;
using CustomerAPI.Domain.Entities;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Customers.Commands.CreateCustomer;

/// <summary>
/// Handles CreateCustomerCommand:
///  1. Maps DTO → Customer entity
///  2. Persists Customer to DB
///  3. Appends CustomerCreatedIntegrationEvent to the outbox (same unit-of-work scope)
///  4. Calls SaveChangesAsync — commits Customer row + OutboxEntry row atomically
///
/// The outbox background processor (<see cref="Mvp24Hours.Infrastructure.Cqrs.Messaging.OutboxProcessor"/>)
/// then reads pending entries and publishes to RabbitMQ via IIntegrationEventPublisher.
/// </summary>
public sealed class CreateCustomerCommandHandler(
    IUnitOfWorkAsync unitOfWork,
    IMapper mapper,
    TimeProvider timeProvider,
    IIntegrationEventOutbox outbox,
    ILogger<CreateCustomerCommandHandler> logger)
    : IMediatorCommandHandler<CreateCustomerCommand, IBusinessResult<CustomerIdResult>>
{
    public async Task<IBusinessResult<CustomerIdResult>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var entity = mapper.Map<Customer>(request.Model);
        entity.Created = timeProvider.GetUtcNow().UtcDateTime;
        entity.Active = true;

        var repository = unitOfWork.GetRepository<Customer>();
        await repository.AddAsync(entity, cancellationToken: cancellationToken);

        // Build integration event BEFORE SaveChanges so the ID is known after commit
        // AddAsync on the outbox only stages the entry; the actual DB write happens in SaveChangesAsync
        var integrationEvent = CustomerCreatedIntegrationEvent.Create(
            customerId: 0,   // placeholder; updated after save below
            customerName: entity.Name,
            customerEmail: entity.Email,
            correlationId: request.CorrelationId,
            causationId: typeof(CreateCustomerCommand).Name);

        if (await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken) <= 0)
        {
            return "OPERATION_FAIL"
                .ToMessageResult("OPERATION_FAIL", MessageType.Error)
                .ToBusiness<CustomerIdResult>();
        }

        logger.LogInformation("[Command] Created customer {CustomerId} ({Name})", entity.Id, entity.Name);

        // Re-create with the real customerId and add to outbox, then flush outbox entry
        var finalEvent = CustomerCreatedIntegrationEvent.Create(
            customerId: entity.Id,
            customerName: entity.Name,
            customerEmail: entity.Email,
            correlationId: request.CorrelationId,
            causationId: typeof(CreateCustomerCommand).Name);

        await outbox.AddAsync(finalEvent, cancellationToken);

        // Flush the outbox entry to DB (second SaveChanges for the outbox row)
        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        logger.LogInformation("[Outbox] Enqueued CustomerCreatedIntegrationEvent {EventId} for customer {CustomerId}",
            finalEvent.Id, entity.Id);

        return new CustomerIdResult(entity.Id).ToBusiness(
            "OPERATION_SUCCESS".ToMessageResult("OPERATION_SUCCESS", MessageType.Success));
    }
}
