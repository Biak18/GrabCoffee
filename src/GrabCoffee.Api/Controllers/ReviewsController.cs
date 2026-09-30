using GrabCoffee.Application.Features.Reviews;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrabCoffee.Api.Controllers;

[ApiController]
[Route("reviews")]
public sealed class ReviewsController(ISender sender) : ControllerBase
{
    [HttpGet("/coffees/{coffeeId:guid}/reviews")]
    [AllowAnonymous]
    public async Task<IActionResult> GetForCoffee(
        Guid coffeeId,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetCoffeeReviewsQuery(coffeeId, limit), ct);
        return Ok(result);
    }

    [HttpGet("/orders/{orderId:guid}/reviewed-coffees")]
    [Authorize]
    public async Task<IActionResult> GetReviewed(Guid orderId, CancellationToken ct)
    {
        var result = await sender.Send(new GetReviewedCoffeeIdsQuery(orderId), ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Submit(
        [FromBody] SubmitReviewRequest request,
        CancellationToken ct)
    {
        await sender.Send(new SubmitReviewCommand(
            request.CoffeeId, request.OrderId, request.Rating, request.Comment), ct);
        return NoContent();
    }

    public sealed record SubmitReviewRequest(
        Guid CoffeeId,
        Guid OrderId,
        int Rating,
        string? Comment);
}
