using GrabCoffee.Application.Features.Profile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrabCoffee.Api.Controllers;

// Own account: display name + self-deletion (delete_account port).
// Avatar upload stays direct-to-Storage on mobile, unchanged.
[ApiController]
[Route("me")]
public sealed class MeController(ISender sender) : ControllerBase
{
    [HttpPatch]
    [Authorize]
    public async Task<IActionResult> UpdateDisplayName(
        [FromBody] UpdateDisplayNameRequest request,
        CancellationToken ct)
    {
        await sender.Send(new UpdateDisplayNameCommand(request.FullName), ct);
        return NoContent();
    }

    [HttpDelete]
    [Authorize]
    public async Task<IActionResult> DeleteAccount(CancellationToken ct)
    {
        await sender.Send(new DeleteAccountCommand(), ct);
        return NoContent();
    }

    public sealed record UpdateDisplayNameRequest(string FullName);
}
