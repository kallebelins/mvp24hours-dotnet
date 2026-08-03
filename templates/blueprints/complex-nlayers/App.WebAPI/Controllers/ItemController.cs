using App.Application;
using App.Core.ValueObjects.Items;
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.DTOs.Models;
using Mvp24Hours.Extensions;

namespace App.WebAPI.Controllers;

[Produces("application/json")]
[Route("api/[controller]")]
[ApiController]
public class ItemController(FacadeService facade) : ControllerBase
{
    [HttpGet]
    [Route("")]
    public async Task<ActionResult<IPagingResult<IList<ItemResult>>>> GetBy(
        [FromQuery] ItemQuery filter,
        [FromQuery] PagingCriteriaRequest pagingCriteria,
        CancellationToken cancellationToken)
    {
        var result = await facade.ItemService.GetBy(filter, pagingCriteria.ToPagingCriteria(), cancellationToken);
        return result.HasData() ? Ok(result) : NotFound(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<IBusinessResult<ItemResult>>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await facade.ItemService.GetById(id, cancellationToken);
        return result.HasData() ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    [Route("")]
    public async Task<ActionResult<IBusinessResult<int>>> Create([FromBody] ItemCreate model, CancellationToken cancellationToken)
    {
        var result = await facade.ItemService.Create(model, cancellationToken);
        return result.HasErrors ? BadRequest(result) : Created(nameof(Create), result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<IBusinessResult<int>>> Update(int id, [FromBody] ItemUpdate model, CancellationToken cancellationToken)
    {
        var result = await facade.ItemService.Update(id, model, cancellationToken);
        return result.HasErrors ? BadRequest(result) : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<IBusinessResult<int>>> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await facade.ItemService.Delete(id, cancellationToken);
        return result.HasErrors ? BadRequest(result) : Ok(result);
    }
}
