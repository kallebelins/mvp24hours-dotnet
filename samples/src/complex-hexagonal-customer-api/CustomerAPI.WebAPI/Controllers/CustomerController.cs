using CustomerAPI.Application.DTOs.Customers;
using CustomerAPI.Application.Ports;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.WebAPI.Controllers;

/// <summary>
/// Inbound HTTP adapter — Customer resource.
/// Routes HTTP requests to the <see cref="ICustomerUseCase"/> inbound port.
/// </summary>
[Produces("application/json")]
[Route("api/[controller]")]
[ApiController]
public class CustomerController(ICustomerUseCase customerUseCase) : ControllerBase
{
    /// <summary>
    /// Get a list of customers (optionally filtered).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IBusinessResult<IList<CustomerResult>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IBusinessResult<IList<CustomerResult>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(IBusinessResult<IList<CustomerResult>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IBusinessResult<IList<CustomerResult>>>> GetAll(
        [FromQuery] CustomerQuery query,
        CancellationToken cancellationToken)
    {
        var result = await customerUseCase.GetCustomersAsync(query, cancellationToken);
        if (result.HasErrors) return BadRequest(result);
        if (result.HasData()) return Ok(result);
        return NotFound(result);
    }

    /// <summary>
    /// Get a single customer with contacts.
    /// </summary>
    [HttpGet("{id:int}", Name = "CustomerGetById")]
    [ProducesResponseType(typeof(IBusinessResult<CustomerIdResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IBusinessResult<CustomerIdResult>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(IBusinessResult<CustomerIdResult>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IBusinessResult<CustomerIdResult>>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await customerUseCase.GetCustomerByIdAsync(id, cancellationToken);
        if (result.HasErrors) return BadRequest(result);
        if (result.HasData()) return Ok(result);
        return NotFound(result);
    }

    /// <summary>
    /// Create a new customer.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(IBusinessResult<int>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IBusinessResult<int>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IBusinessResult<int>>> Create(
        [FromBody] CustomerCreate dto,
        CancellationToken cancellationToken)
    {
        var result = await customerUseCase.CreateCustomerAsync(dto, cancellationToken);
        if (result.HasErrors) return BadRequest(result);
        return CreatedAtRoute("CustomerGetById", new { id = result.Data }, result);
    }

    /// <summary>
    /// Update an existing customer.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(IBusinessResult<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IBusinessResult<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(IBusinessResult<bool>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IBusinessResult<bool>>> Update(
        int id,
        [FromBody] CustomerUpdate dto,
        CancellationToken cancellationToken)
    {
        var result = await customerUseCase.UpdateCustomerAsync(id, dto, cancellationToken);
        if (result.HasErrors) return result.Data ? Ok(result) : NotFound(result);
        return Ok(result);
    }

    /// <summary>
    /// Delete a customer.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(IBusinessResult<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IBusinessResult<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(IBusinessResult<bool>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IBusinessResult<bool>>> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await customerUseCase.DeleteCustomerAsync(id, cancellationToken);
        if (result.HasErrors) return result.Data ? Ok(result) : NotFound(result);
        return Ok(result);
    }
}
