using CustomerAPI.Application.Contacts.Commands.AddContact;
using CustomerAPI.Application.Contacts.Commands.RemoveContact;
using CustomerAPI.Application.Contacts.Queries.GetContactsByCustomer;
using CustomerAPI.Core.Resources;
using CustomerAPI.Core.ValueObjects.Contacts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.WebAPI.Controllers;

/// <summary>
/// Contact resources nested under a Customer aggregate.
/// Write operations go through the aggregate root (Customer) — not the Contact repository directly.
/// </summary>
[Produces("application/json")]
[Route("api/Customer")]
[ApiController]
public class ContactController(IMediator mediator) : ControllerBase
{
    /// <summary>Get contacts for a customer</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<IList<ContactIdResult>>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<IList<ContactIdResult>>>), StatusCodes.Status404NotFound)]
    [Route("{customerId:int}/Contact", Name = "ContactGetBy")]
    public async Task<ActionResult<IBusinessResult<IList<ContactIdResult>>>> GetBy(int customerId, CancellationToken cancellationToken)
    {
        var result = await mediator.SendAsync(new GetContactsByCustomerQuery { CustomerId = customerId }, cancellationToken);
        return result.HasData() ? Ok(result) : NotFound(result);
    }

    /// <summary>Add contact to customer — calls Customer.AddContact() aggregate method</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status400BadRequest)]
    [Route("{customerId:int}/Contact", Name = "ContactAdd")]
    public async Task<ActionResult<IBusinessResult<int>>> Add(
        int customerId,
        [FromBody] ContactCreate model,
        CancellationToken cancellationToken)
    {
        var result = await mediator.SendAsync(new AddContactCommand
        {
            CustomerId = customerId,
            Model = model
        }, cancellationToken);

        return result.HasErrors ? BadRequest(result) : Created(nameof(Add), result);
    }

    /// <summary>Remove contact from customer — calls Customer.RemoveContact() aggregate method</summary>
    [HttpDelete]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status400BadRequest)]
    [Route("{customerId:int}/Contact/{id}", Name = "ContactRemove")]
    public async Task<ActionResult<IBusinessResult<int>>> Remove(int customerId, int id, CancellationToken cancellationToken)
    {
        var result = await mediator.SendAsync(new RemoveContactCommand
        {
            CustomerId = customerId,
            Id = id
        }, cancellationToken);

        if (result.HasErrors)
        {
            return result.HasMessageKey(nameof(Messages.RECORD_NOT_FOUND_FOR_ID))
                ? NotFound(result)
                : BadRequest(result);
        }

        return Ok(result);
    }
}
