using System.Net;
using CustomerAPI.Application;
using CustomerAPI.Core.Resources;
using CustomerAPI.Core.ValueObjects.Contacts;
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;

namespace CustomerAPI.WebAPI.Controllers;

/// <summary>
/// Contact HTTP resources nested under a customer, with DTO contracts.
/// </summary>
[Produces("application/json")]
[Route("api/Customer")]
[ApiController]
public class ContactController(FacadeService facade) : ControllerBase
{
    #region [ Actions / Resources ]

    /// <summary>
    /// Get contacts for a customer
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<IList<ContactIdResult>>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<IList<ContactIdResult>>>), StatusCodes.Status404NotFound)]
    [Route("{customerId:int}/Contact", Name = "ContactGetBy")]
    public async Task<ActionResult<IBusinessResult<IList<ContactIdResult>>>> GetBy(int customerId, CancellationToken cancellationToken)
    {
        IBusinessResult<IList<ContactIdResult>> result = await facade.ContactService.GetBy(customerId, cancellationToken: cancellationToken);
        if (result.HasData())
        {
            return Ok(result);
        }
        return NotFound(result);
    }

    /// <summary>
    /// Create contact for customer
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status400BadRequest)]
    [Route("{customerId:int}/Contact", Name = "ContactCreate")]
    public async Task<ActionResult<IBusinessResult<int>>> Create(int customerId, [FromBody] ContactCreate model, CancellationToken cancellationToken)
    {
        IBusinessResult<int> result = await facade.ContactService.Create(customerId, model, cancellationToken: cancellationToken);
        if (result.HasErrors)
        {
            return BadRequest(result);
        }
        return Created(nameof(Create), result);
    }

    /// <summary>
    /// Update customer contact
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [Route("{customerId:int}/Contact/{id:int}", Name = "ContactUpdate")]
    public async Task<ActionResult<IBusinessResult<int>>> Update(int customerId, int id, [FromBody] ContactUpdate model, CancellationToken cancellationToken)
    {
        IBusinessResult<int> result = await facade.ContactService.Update(customerId, id, model, cancellationToken: cancellationToken);
        if (result.HasErrors)
        {
            if (result.HasMessageKey(nameof(Messages.RECORD_NOT_FOUND_FOR_ID)))
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }
        else if (result.GetDataValue() == 0)
        {
            return StatusCode((int)HttpStatusCode.NotModified);
        }
        return Ok(result);
    }

    /// <summary>
    /// Delete customer contact
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status400BadRequest)]
    [Route("{customerId:int}/Contact/{id:int}", Name = "ContactDelete")]
    public async Task<ActionResult<IBusinessResult<int>>> Delete(int customerId, int id, CancellationToken cancellationToken)
    {
        IBusinessResult<int> result = await facade.ContactService.Delete(customerId, id, cancellationToken: cancellationToken);
        if (result.HasErrors)
        {
            if (result.HasMessageKey(nameof(Messages.RECORD_NOT_FOUND_FOR_ID)))
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }
        return Ok(result);
    }

    #endregion
}
