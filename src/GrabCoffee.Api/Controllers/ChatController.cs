using GrabCoffee.Application.Features.Chat;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrabCoffee.Api.Controllers;

[ApiController]
[Route("orders")]
public sealed class ChatController(ISender sender) : ControllerBase
{
    [HttpGet("{orderId:guid}/messages")]
    [Authorize]
    public async Task<IActionResult> GetMessages(
        Guid orderId,
        [FromQuery] int limit = 50,
        [FromQuery] DateTime? before = null,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetOrderMessagesQuery(orderId, limit, before), ct);
        return Ok(result);
    }

    [HttpPost("{orderId:guid}/messages")]
    [Authorize]
    public async Task<IActionResult> SendMessage(
        Guid orderId,
        [FromBody] SendMessageRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new SendOrderMessageCommand(orderId, request.Body), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    public sealed record SendMessageRequest(string Body);
}
