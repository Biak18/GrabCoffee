using GrabCoffee.Application.Features.PushTokens;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrabCoffee.Api.Controllers;

[ApiController]
[Route("push-tokens")]
public sealed class PushTokensController(ISender sender) : ControllerBase
{
    [HttpPut]
    [Authorize]
    public async Task<IActionResult> Register(
        [FromBody] RegisterPushTokenRequest request,
        CancellationToken ct)
    {
        await sender.Send(new RegisterPushTokenCommand(request.Token), ct);
        return NoContent();
    }

    [HttpDelete]
    [Authorize]
    public async Task<IActionResult> Unregister(CancellationToken ct)
    {
        await sender.Send(new UnregisterPushTokensCommand(), ct);
        return NoContent();
    }

    public sealed record RegisterPushTokenRequest(string Token);
}
