using App.Application.Items.Commands.CreateItem;
using App.Application.Items.Commands.DeleteItem;
using App.Application.Items.Commands.UpdateItem;
using App.Application.Items.Queries.GetItemById;
using App.Application.Items.Queries.GetItems;
using App.Core.ValueObjects.Items;
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.DTOs.Models;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace App.WebAPI.Controllers;

[Produces("application/json")]
[Route("api/[controller]")]
[ApiController]
public class ItemController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Route("")]
    public async Task<ActionResult<IPagingResult<IList<ItemResult>>>> GetBy(
        [FromQuery] ItemQuery filter,
        [FromQuery] PagingCriteriaRequest pagingCriteria,
        CancellationToken cancellationToken)
    {
        var query = new GetItemsQuery
        {
            Filter = filter,
            Paging = pagingCriteria.ToPagingCriteria()
        };
        var result = await mediator.SendAsync(query, cancellationToken);
        return result.HasData() ? Ok(result) : NotFound(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<IBusinessResult<ItemResult>>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.SendAsync(new GetItemByIdQuery { Id = id }, cancellationToken);
        return result.HasData() ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    [Route("")]
    public async Task<ActionResult<IBusinessResult<int>>> Create([FromBody] ItemCreate model, CancellationToken cancellationToken)
    {
        var command = new CreateItemCommand { Name = model.Name, Note = model.Note };
        var result = await mediator.SendAsync(command, cancellationToken);
        return result.HasErrors ? BadRequest(result) : Created(nameof(Create), result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<IBusinessResult<int>>> Update(int id, [FromBody] ItemUpdate model, CancellationToken cancellationToken)
    {
        var command = new UpdateItemCommand { Id = id, Name = model.Name, Note = model.Note };
        var result = await mediator.SendAsync(command, cancellationToken);
        return result.HasErrors ? BadRequest(result) : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<IBusinessResult<int>>> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.SendAsync(new DeleteItemCommand { Id = id }, cancellationToken);
        return result.HasErrors ? BadRequest(result) : Ok(result);
    }
}
