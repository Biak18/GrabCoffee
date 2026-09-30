using GrabCoffee.Application.Features.Promotions;
using GrabCoffee.Application.Features.Promotions.GetActivePromotions;
using GrabCoffee.Application.Features.Promotions.LookupPromoCode;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrabCoffee.Api.Controllers;

[ApiController]
[Route("promotions")]
public sealed class PromotionsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetActive(
        [FromQuery] Guid? storeId,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetActivePromotionsQuery(storeId), ct);
        return Ok(result);
    }

    [HttpGet("lookup")]
    [AllowAnonymous]
    public async Task<IActionResult> Lookup(
        [FromQuery] Guid storeId,
        [FromQuery] string code,
        CancellationToken ct)
    {
        var result = await sender.Send(new LookupPromoCodeQuery(storeId, code), ct);
        return result is null ? NotFound() : Ok(result);
    }
}
