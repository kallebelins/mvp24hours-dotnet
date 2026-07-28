using CustomerAPI.Models;
using CustomerAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CustomerAPI.Controllers;

/// <summary>
/// Manages customer resources.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CustomersController(ICustomerService customerService, ILogger<CustomersController> logger) : ControllerBase
{
    /// <summary>
    /// Returns all active customers.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IEnumerable<CustomerResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var customers = await customerService.GetAllAsync(ct);
        return Ok(customers);
    }

    /// <summary>
    /// Returns a single customer by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var customer = await customerService.GetByIdAsync(id, ct);
        return customer is null ? NotFound() : Ok(customer);
    }

    /// <summary>
    /// Creates a new customer and publishes a CustomerCreatedEvent to RabbitMQ.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        logger.LogInformation("Creating customer: {Email}", request.Email);
        var response = await customerService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }
}
