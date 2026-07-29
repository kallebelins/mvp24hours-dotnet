using System.Net;
using CustomerAPI.Application.Customers.Commands.CreateCustomer;
using CustomerAPI.Application.Customers.Commands.DeleteCustomer;
using CustomerAPI.Application.Customers.Commands.UpdateCustomer;
using CustomerAPI.Application.Customers.Queries.GetCustomerById;
using CustomerAPI.Application.Customers.Queries.GetCustomers;
using CustomerAPI.Core.Resources;
using CustomerAPI.Core.ValueObjects.Customers;
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.DTOs.Models;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.WebAPI.Controllers;

/// <summary>
/// Customer HTTP resources dispatched through the Mvp24Hours Mediator (CQRS).
/// </summary>
[Produces("application/json")]
[Route("api/[controller]")]
[ApiController]
public class CustomerController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ActionResult<IPagingResult<IList<CustomerResult>>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ActionResult<IPagingResult<IList<CustomerResult>>>), StatusCodes.Status404NotFound)]
    [Route("", Name = "CustomerGetBy")]
    public async Task<ActionResult<IPagingResult<IList<CustomerResult>>>> GetBy(
        [FromQuery] CustomerQuery filter,
        [FromQuery] PagingCriteriaRequest pagingCriteria,
        CancellationToken cancellationToken)
    {
        IPagingResult<IList<CustomerResult>> result = await mediator.SendAsync(new GetCustomersQuery
        {
            Filter = filter,
            Criteria = pagingCriteria.ToPagingCriteria()
        }, cancellationToken);

        if (result.HasData())
        {
            return Ok(result);
        }

        return NotFound(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<CustomerIdResult>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<CustomerIdResult>>), StatusCodes.Status404NotFound)]
    [Route("{id:int}", Name = "CustomerGetById")]
    public async Task<ActionResult<IBusinessResult<CustomerIdResult>>> GetById(int id, CancellationToken cancellationToken)
    {
        IBusinessResult<CustomerIdResult> result = await mediator.SendAsync(new GetCustomerByIdQuery { Id = id }, cancellationToken);
        if (result.HasData())
        {
            return Ok(result);
        }

        return NotFound(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status400BadRequest)]
    [Route("", Name = "CustomerCreate")]
    public async Task<ActionResult<IBusinessResult<int>>> Create([FromBody] CustomerCreate model, CancellationToken cancellationToken)
    {
        IBusinessResult<int> result = await mediator.SendAsync(new CreateCustomerCommand { Model = model }, cancellationToken);
        if (result.HasErrors)
        {
            return BadRequest(result);
        }

        return Created(nameof(Create), result);
    }

    [HttpPut]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [Route("{id:int}", Name = "CustomerUpdate")]
    public async Task<ActionResult<IBusinessResult<int>>> Update(int id, [FromBody] CustomerUpdate model, CancellationToken cancellationToken)
    {
        IBusinessResult<int> result = await mediator.SendAsync(new UpdateCustomerCommand { Id = id, Model = model }, cancellationToken);
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
    [Route("{id:int}", Name = "CustomerDelete")]
    public async Task<ActionResult<IBusinessResult<int>>> Delete(int id, CancellationToken cancellationToken)
    {
        IBusinessResult<int> result = await mediator.SendAsync(new DeleteCustomerCommand { Id = id }, cancellationToken);
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
