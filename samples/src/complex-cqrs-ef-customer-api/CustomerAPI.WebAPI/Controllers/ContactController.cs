using CustomerAPI.Application.Contacts.Commands.CreateContact;
using CustomerAPI.Application.Contacts.Commands.DeleteContact;
using CustomerAPI.Application.Contacts.Commands.UpdateContact;
using CustomerAPI.Application.Contacts.Queries.GetContactsByCustomer;
using CustomerAPI.Core.Resources;
using CustomerAPI.Core.ValueObjects.Contacts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.WebAPI.Controllers;

/// <summary>
/// Contact HTTP resources nested under a customer, dispatched through the Mvp24Hours Mediator.
/// </summary>
[Produces("application/json")]
[Route("api/Customer")]
[ApiController]
public class ContactController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<IList<ContactIdResult>>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<IList<ContactIdResult>>>), StatusCodes.Status404NotFound)]
    [Route("{customerId:int}/Contact", Name = "ContactGetBy")]
    public async Task<ActionResult<IBusinessResult<IList<ContactIdResult>>>> GetBy(int customerId, CancellationToken cancellationToken)
    {
        var result = await mediator.SendAsync(new GetContactsByCustomerQuery { CustomerId = customerId }, cancellationToken);
        if (result.HasData())
        {
            return Ok(result);
        }

        return NotFound(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status400BadRequest)]
    [Route("{customerId:int}/Contact", Name = "ContactCreate")]
    public async Task<ActionResult<IBusinessResult<int>>> Create(
        int customerId,
        [FromBody] ContactCreate model,
        CancellationToken cancellationToken)
    {
        var result = await mediator.SendAsync(new CreateContactCommand
        {
            CustomerId = customerId,
            Model = model
        }, cancellationToken);

        if (result.HasErrors)
        {
            return BadRequest(result);
        }

        return Created(nameof(Create), result);
    }

    [HttpPut]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [Route("{customerId:int}/Contact/{id}", Name = "ContactUpdate")]
    public async Task<ActionResult<IBusinessResult<int>>> Update(
        int customerId,
        int id,
        [FromBody] ContactUpdate model,
        CancellationToken cancellationToken)
    {
        var result = await mediator.SendAsync(new UpdateContactCommand
        {
            CustomerId = customerId,
            Id = id,
            Model = model
        }, cancellationToken);

        if (result.HasErrors)
        {
            if (result.HasMessageKey(nameof(Messages.RECORD_NOT_FOUND_FOR_ID)))
            {
                return NotFound(result);
            }

            return BadRequest(result);
        }

        if (result.GetDataValue() == 0)
        {
            return StatusCode((int)HttpStatusCode.NotModified);
        }

        return Ok(result);
    }

    [HttpDelete]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status400BadRequest)]
    [Route("{customerId:int}/Contact/{id}", Name = "ContactDelete")]
    public async Task<ActionResult<IBusinessResult<int>>> Delete(int customerId, int id, CancellationToken cancellationToken)
    {
        var result = await mediator.SendAsync(new DeleteContactCommand
        {
            CustomerId = customerId,
            Id = id
        }, cancellationToken);

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
}
