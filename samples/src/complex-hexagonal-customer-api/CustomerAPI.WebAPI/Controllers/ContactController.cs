using CustomerAPI.Application.DTOs.Contacts;
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
/// Inbound HTTP adapter — Contact sub-resource nested under Customer.
/// Routes HTTP requests to the <see cref="ICustomerUseCase"/> inbound port.
/// </summary>
[Produces("application/json")]
[Route("api/customer/{customerId:int}/contact")]
[ApiController]
public class ContactController(ICustomerUseCase customerUseCase) : ControllerBase
{
    /// <summary>
    /// Get all contacts for a customer.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IBusinessResult<IList<ContactResult>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IBusinessResult<IList<ContactResult>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(IBusinessResult<IList<ContactResult>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IBusinessResult<IList<ContactResult>>>> GetAll(
        int customerId,
        CancellationToken cancellationToken)
    {
        var result = await customerUseCase.GetContactsAsync(customerId, cancellationToken);
        if (result.HasErrors) return BadRequest(result);
        if (result.HasData()) return Ok(result);
        return NotFound(result);
    }

    /// <summary>
    /// Add a contact to a customer.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(IBusinessResult<int>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IBusinessResult<int>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(IBusinessResult<int>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IBusinessResult<int>>> Create(
        int customerId,
        [FromBody] ContactCreate dto,
        CancellationToken cancellationToken)
    {
        var result = await customerUseCase.CreateContactAsync(customerId, dto, cancellationToken);
        if (result.HasErrors) return result.Data > 0 ? Created(string.Empty, result) : NotFound(result);
        return Created(string.Empty, result);
    }

    /// <summary>
    /// Update a contact.
    /// </summary>
    [HttpPut("{contactId:int}")]
    [ProducesResponseType(typeof(IBusinessResult<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IBusinessResult<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(IBusinessResult<bool>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IBusinessResult<bool>>> Update(
        int customerId,
        int contactId,
        [FromBody] ContactUpdate dto,
        CancellationToken cancellationToken)
    {
        var result = await customerUseCase.UpdateContactAsync(customerId, contactId, dto, cancellationToken);
        if (result.HasErrors) return result.Data ? Ok(result) : NotFound(result);
        return Ok(result);
    }

    /// <summary>
    /// Delete a contact.
    /// </summary>
    [HttpDelete("{contactId:int}")]
    [ProducesResponseType(typeof(IBusinessResult<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IBusinessResult<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(IBusinessResult<bool>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IBusinessResult<bool>>> Delete(
        int customerId,
        int contactId,
        CancellationToken cancellationToken)
    {
        var result = await customerUseCase.DeleteContactAsync(customerId, contactId, cancellationToken);
        if (result.HasErrors) return result.Data ? Ok(result) : NotFound(result);
        return Ok(result);
    }
}
