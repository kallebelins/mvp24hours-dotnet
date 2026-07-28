using CustomerAPI.Core.DTOs;
using CustomerAPI.Core.Entities;
using CustomerAPI.WebAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerAPI.WebAPI.Controllers;

/// <summary>
/// Customer read operations. All endpoints require a valid Keycloak JWT.
/// </summary>
[ApiController]
[Route("api/customers")]
[Authorize]
public sealed class CustomerController(InMemoryCustomerStore store) : ControllerBase
{
    /// <summary>
    /// Returns all active customers.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetAll()
    {
        IReadOnlyList<CustomerResult> results = store.GetAll()
            .Select(MapToResult)
            .ToList();
        return Ok(results);
    }

    /// <summary>
    /// Returns a single customer by identifier.
    /// </summary>
    /// <param name="id">Customer GUID.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(Guid id)
    {
        Customer? customer = store.GetById(id);
        return customer is null ? NotFound() : Ok(MapToResult(customer));
    }

    private static CustomerResult MapToResult(Customer c) =>
        new(c.Id, c.Name, c.Email, c.Active, c.CreatedAt);
}
