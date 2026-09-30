using GrabCoffee.Application.Features.Coffees.GetCoffeeById;
using GrabCoffee.Application.Features.Coffees.GetCoffees;
using GrabCoffee.Application.Features.Coffees.GetHighlights;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrabCoffee.Api.Controllers;

[ApiController]
[Route("coffees")]
public sealed class CoffeesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetCoffees(
        [FromQuery] Guid? storeId,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? search,
        [FromQuery] string sort = "popular",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(
            new GetCoffeesQuery(storeId, categoryId, search, sort, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("highlights")]
    [AllowAnonymous]
    public async Task<IActionResult> GetHighlights(CancellationToken ct)
    {
        var result = await sender.Send(new GetCoffeeHighlightsQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetCoffeeByIdQuery(id), ct);
        return Ok(result);
    }
}
