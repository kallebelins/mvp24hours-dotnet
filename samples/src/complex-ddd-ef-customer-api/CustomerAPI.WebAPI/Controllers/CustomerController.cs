using CustomerAPI.Application.Customers.Commands.CreateCustomer;
using CustomerAPI.Application.Customers.Commands.DeactivateCustomer;
using CustomerAPI.Application.Customers.Commands.UpdateCustomer;
using CustomerAPI.Application.Customers.Queries.GetCustomerById;
using CustomerAPI.Application.Customers.Queries.GetCustomers;
using CustomerAPI.Core.Resources;
using CustomerAPI.Core.ValueObjects.Customers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.DTOs.Models;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.WebAPI.Controllers;

/// <summary>
/// Customer HTTP resources dispatched through the Mvp24Hours Mediator (DDD + CQRS).
/// All write operations flow through Customer aggregate methods — no anemic model updates.
/// </summary>
[Produces("application/json")]
[Route("api/[controller]")]
[ApiController]
public class CustomerController(IMediator mediator) : ControllerBase
{
    /// <summary>Get paginated list of customers</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ActionResult<IPagingResult<IList<CustomerResult>>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ActionResult<IPagingResult<IList<CustomerResult>>>), StatusCodes.Status404NotFound)]
    [Route("", Name = "CustomerGetBy")]
    public async Task<ActionResult<IPagingResult<IList<CustomerResult>>>> GetBy(
        [FromQuery] CustomerQuery filter,
        [FromQuery] PagingCriteriaRequest pagingCriteria,
        CancellationToken cancellationToken)
    {
        var result = await mediator.SendAsync(new GetCustomersQuery
        {
            Filter = filter,
            Criteria = pagingCriteria.ToPagingCriteria()
        }, cancellationToken);

        return result.HasData() ? Ok(result) : NotFound(result);
    }

    /// <summary>Get customer with contact list</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<CustomerIdResult>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<CustomerIdResult>>), StatusCodes.Status404NotFound)]
    [Route("{id}", Name = "CustomerGetById")]
    public async Task<ActionResult<IBusinessResult<CustomerIdResult>>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.SendAsync(new GetCustomerByIdQuery { Id = id }, cancellationToken);
        return result.HasData() ? Ok(result) : NotFound(result);
    }

    /// <summary>Create customer — calls Customer.Create() aggregate factory</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status400BadRequest)]
    [Route("", Name = "CustomerCreate")]
    public async Task<ActionResult<IBusinessResult<int>>> Create([FromBody] CustomerCreate model, CancellationToken cancellationToken)
    {
        var result = await mediator.SendAsync(new CreateCustomerCommand { Model = model }, cancellationToken);
        return result.HasErrors ? BadRequest(result) : Created(nameof(Create), result);
    }

    /// <summary>Update customer name/note — calls Rename() and UpdateNote() aggregate methods</summary>
    [HttpPut]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [Route("{id}", Name = "CustomerUpdate")]
    public async Task<ActionResult<IBusinessResult<int>>> Update(int id, [FromBody] CustomerUpdate model, CancellationToken cancellationToken)
    {
        var result = await mediator.SendAsync(new UpdateCustomerCommand { Id = id, Model = model }, cancellationToken);
        if (result.HasErrors)
        {
            return result.HasMessageKey(nameof(Messages.RECORD_NOT_FOUND_FOR_ID))
                ? NotFound(result)
                : BadRequest(result);
        }

        if (result.GetDataValue() == 0)
            return StatusCode((int)HttpStatusCode.NotModified);

        return Ok(result);
    }

    /// <summary>Deactivate customer — calls Customer.Deactivate() aggregate method</summary>
    [HttpDelete]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ActionResult<IBusinessResult<int>>), StatusCodes.Status404NotFound)]
    [Route("{id}", Name = "CustomerDeactivate")]
    public async Task<ActionResult<IBusinessResult<int>>> Deactivate(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.SendAsync(new DeactivateCustomerCommand { Id = id }, cancellationToken);
        if (result.HasErrors)
        {
            return result.HasMessageKey(nameof(Messages.RECORD_NOT_FOUND_FOR_ID))
                ? NotFound(result)
                : BadRequest(result);
        }

        return Ok(result);
    }
}
