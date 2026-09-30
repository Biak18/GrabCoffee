using GrabCoffee.Application.Features.Favorites;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrabCoffee.Api.Controllers;

[ApiController]
[Route("favorites")]
public sealed class FavoritesController(ISender sender) : ControllerBase
{
    [HttpGet("coffees")]
    [Authorize]
    public async Task<IActionResult> GetCoffees(CancellationToken ct)
    {
        var result = await sender.Send(new GetFavoriteCoffeesQuery(), ct);
        return Ok(result);
    }

    [HttpPost("coffees/{coffeeId:guid}")]
    [Authorize]
    public async Task<IActionResult> LikeCoffee(Guid coffeeId, CancellationToken ct)
    {
        await sender.Send(new ToggleCoffeeFavoriteCommand(coffeeId, Liked: true), ct);
        return NoContent();
    }

    [HttpDelete("coffees/{coffeeId:guid}")]
    [Authorize]
    public async Task<IActionResult> UnlikeCoffee(Guid coffeeId, CancellationToken ct)
    {
        await sender.Send(new ToggleCoffeeFavoriteCommand(coffeeId, Liked: false), ct);
        return NoContent();
    }

    [HttpGet("stores")]
    [Authorize]
    public async Task<IActionResult> GetStores(CancellationToken ct)
    {
        var result = await sender.Send(new GetFavoriteStoresQuery(), ct);
        return Ok(result);
    }

    [HttpPost("stores/{storeId:guid}")]
    [Authorize]
    public async Task<IActionResult> LikeStore(Guid storeId, CancellationToken ct)
    {
        await sender.Send(new ToggleStoreFavoriteCommand(storeId, Liked: true), ct);
        return NoContent();
    }

    [HttpDelete("stores/{storeId:guid}")]
    [Authorize]
    public async Task<IActionResult> UnlikeStore(Guid storeId, CancellationToken ct)
    {
        await sender.Send(new ToggleStoreFavoriteCommand(storeId, Liked: false), ct);
        return NoContent();
    }
}
