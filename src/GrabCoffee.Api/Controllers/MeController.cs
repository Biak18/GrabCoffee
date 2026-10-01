using GrabCoffee.Application.Features.Profile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrabCoffee.Api.Controllers;

// Own account: profile update (name + avatar URL from POST /uploads/avatar)
// and self-deletion (delete_account port).
[ApiController]
[Route("me")]
public sealed class MeController(ISender sender) : ControllerBase
{
    [HttpPatch]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken ct)
    {
        await sender.Send(new UpdateProfileCommand(request.FullName, request.AvatarUrl), ct);
        return NoContent();
    }

    [HttpDelete]
    [Authorize]
    public async Task<IActionResult> DeleteAccount(CancellationToken ct)
    {
        await sender.Send(new DeleteAccountCommand(), ct);
        return NoContent();
    }

    public sealed record UpdateProfileRequest(string? FullName, string? AvatarUrl);
}
