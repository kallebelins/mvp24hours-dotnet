using App.Application.DTOs;
using App.Application.UseCases;
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;

namespace App.WebAPI.Controllers;

[Produces("application/json")]
[Route("api/[controller]")]
[ApiController]
public class ItemController(IItemUseCase useCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IBusinessResult<List<ItemResult>>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await useCase.GetAllAsync(cancellationToken);
        return result.HasData() ? Ok(result) : NotFound(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<IBusinessResult<ItemResult>>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await useCase.GetByIdAsync(id, cancellationToken);
        return result.HasData() ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<ActionResult<IBusinessResult<int>>> Create([FromBody] ItemCreate model, CancellationToken cancellationToken)
    {
        var result = await useCase.CreateAsync(model, cancellationToken);
        return result.HasErrors ? BadRequest(result) : Created(nameof(Create), result);
    }
}
