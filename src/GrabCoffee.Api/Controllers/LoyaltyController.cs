using GrabCoffee.Application.Features.Loyalty.GetMyCards;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrabCoffee.Api.Controllers;

[ApiController]
[Route("loyalty")]
public sealed class LoyaltyController(ISender sender) : ControllerBase
{
    [HttpGet("cards")]
    [Authorize]
    public async Task<IActionResult> GetMyCards(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyCardsQuery(), ct);
        return Ok(result);
    }
}
