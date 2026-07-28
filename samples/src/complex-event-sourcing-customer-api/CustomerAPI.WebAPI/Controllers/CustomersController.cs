using CustomerAPI.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CustomerAPI.WebAPI.Controllers;

/// <summary>
/// Manages customers via an event-sourced aggregate.
/// All writes append events; reads are served from the in-memory projection.
/// </summary>
[ApiController]
[Route("api/customers")]
public class CustomersController(CustomerEventStoreService customerService) : ControllerBase
{
    /// <summary>
    /// Creates a new customer and appends a CustomerCreated event.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        Guid id = await customerService.CreateAsync(request.Name, request.Email, cancellationToken);
        var model = customerService.GetById(id);
        return CreatedAtAction(nameof(GetById), new { id }, model);
    }

    /// <summary>
    /// Renames a customer and appends a CustomerRenamed event.
    /// </summary>
    [HttpPut("{id:guid}/name")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Rename(
        Guid id,
        [FromBody] RenameCustomerRequest request,
        CancellationToken cancellationToken)
    {
        await customerService.RenameAsync(id, request.NewName, cancellationToken);
        return Ok(customerService.GetById(id));
    }

    /// <summary>
    /// Deactivates a customer and appends a CustomerDeactivated event.
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        await customerService.DeactivateAsync(id, cancellationToken);
        return Ok(customerService.GetById(id));
    }

    /// <summary>
    /// Returns a customer from the in-memory projection (fast read, no event replay).
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(Guid id)
    {
        var model = customerService.GetById(id);
        return model is null ? NotFound() : Ok(model);
    }

    /// <summary>
    /// Returns all customers from the projection.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAll() => Ok(customerService.GetAll());

    /// <summary>
    /// Rehydrates the aggregate from the event store and returns its current state.
    /// Demonstrates full event replay without using the projection.
    /// </summary>
    [HttpGet("{id:guid}/rehydrate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Rehydrate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var aggregate = await customerService.RehydrateAsync(id, cancellationToken);
        if (aggregate is null)
            return NotFound();

        return Ok(new
        {
            aggregate.Id,
            aggregate.Name,
            aggregate.Email,
            aggregate.IsActive,
            aggregate.Version,
            UncommittedEvents = aggregate.UncommittedEvents.Count
        });
    }
}

public record CreateCustomerRequest(string Name, string Email);
public record RenameCustomerRequest(string NewName);
