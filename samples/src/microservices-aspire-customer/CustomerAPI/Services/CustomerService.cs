using CustomerAPI.Data;
using CustomerAPI.Entities;
using CustomerAPI.Events;
using CustomerAPI.Models;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;

namespace CustomerAPI.Services;

/// <summary>
/// Handles customer creation and retrieval.
/// On create, publishes a <see cref="CustomerCreatedEvent"/> to RabbitMQ so downstream
/// services (e.g., NotificationWorker) can react without coupling.
/// </summary>
public class CustomerService(
    CustomerDbContext db,
    IMvpRabbitMQClient rabbitClient,
    ILogger<CustomerService> logger) : ICustomerService
{
    public async Task<IEnumerable<CustomerResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.Customers
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => ToResponse(c))
            .ToListAsync(ct);
    }

    public async Task<CustomerResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        return customer is null ? null : ToResponse(customer);
    }

    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        var customer = new Customer
        {
            Name = request.Name,
            Email = request.Email
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Customer created: {CustomerId} ({Name})", customer.Id, customer.Name);

        // Publish integration event — fire-and-forget after successful DB write.
        var @event = new CustomerCreatedEvent(customer.Id, customer.Name, customer.Email, customer.CreatedAt);
        try
        {
            await rabbitClient.PublishAsync(@event, nameof(CustomerCreatedEvent), cancellationToken: ct);
            logger.LogInformation("Published {Event} for customer {CustomerId}", nameof(CustomerCreatedEvent), customer.Id);
        }
        catch (Exception ex)
        {
            // Log and continue — DB write already succeeded, message can be retried/outboxed.
            logger.LogWarning(ex, "Failed to publish {Event} for customer {CustomerId}", nameof(CustomerCreatedEvent), customer.Id);
        }

        return ToResponse(customer);
    }

    private static CustomerResponse ToResponse(Customer c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Email = c.Email,
        CreatedAt = c.CreatedAt,
        IsActive = c.IsActive
    };
}
