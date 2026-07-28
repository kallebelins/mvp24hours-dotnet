using CustomerAPI.Application.Customers.Commands.CreateCustomer;
using CustomerAPI.Application.Customers.Queries.GetCustomers;
using CustomerAPI.Application.DTOs.Customers;
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.WebAPI.Controllers;

/// <summary>
/// Customer management endpoints.
/// POST /customers demonstrates the full Outbox → RabbitMQ → Inbox flow.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CustomerController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// List customers.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IBusinessResult<IList<CustomerResult>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll([FromQuery] string? name, [FromQuery] bool? active, CancellationToken cancellationToken)
    {
        var query = new GetCustomersQuery { Name = name, Active = active };
        var result = await mediator.SendAsync(query, cancellationToken);

        if (result.HasErrors)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a customer.
    /// Writes Customer + OutboxEntry in the same DB scope.
    /// The OutboxProcessor background service publishes to RabbitMQ, and the consumer
    /// writes a NotificationLog (with inbox idempotency protection).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(IBusinessResult<CustomerIdResult>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CustomerCreate model, CancellationToken cancellationToken)
    {
        // Propagate X-Correlation-Id header for distributed tracing
        var correlationId = HttpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        var command = new CreateCustomerCommand(model, correlationId);
        var result = await mediator.SendAsync(command, cancellationToken);

        if (result.HasErrors)
        {
            return BadRequest(result);
        }

        return StatusCode(StatusCodes.Status201Created, result);
    }
}
