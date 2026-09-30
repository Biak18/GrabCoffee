using GrabCoffee.Application.Features.Drivers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrabCoffee.Api.Controllers;

[ApiController]
[Route("drivers")]
public sealed class DriversController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    [Authorize]
    public async Task<IActionResult> Register(
        [FromBody] RegisterDriverRequest request,
        CancellationToken ct)
    {
        await sender.Send(new RegisterDriverCommand(
            request.FullName, request.Phone, request.Vehicle), ct);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyDriverQuery(), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPatch("me/availability")]
    [Authorize]
    public async Task<IActionResult> SetAvailability(
        [FromBody] SetAvailabilityRequest request,
        CancellationToken ct)
    {
        await sender.Send(new SetAvailabilityCommand(request.IsAvailable), ct);
        return NoContent();
    }

    [HttpGet("available")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> GetAvailable(CancellationToken ct)
    {
        // Seller-only: driver names/phones are exposed here for assignment.
        var result = await sender.Send(new GetAvailableDriversQuery(), ct);
        return Ok(result);
    }

    public sealed record RegisterDriverRequest(
        string? FullName,
        string? Phone,
        string? Vehicle);

    public sealed record SetAvailabilityRequest(bool IsAvailable);
}
