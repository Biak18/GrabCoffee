using GrabCoffee.Application.Features.Uploads;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrabCoffee.Api.Controllers;

// Image uploads via Cloudinary. Mobile flow: POST here, then store the
// returned URL via PATCH /me (avatar) or the coffee endpoints.
[ApiController]
[Route("uploads")]
public sealed class UploadsController(ISender sender) : ControllerBase
{
    private const long MaxBytes = 5_000_000;

    [HttpPost("avatar")]
    [Authorize]
    [RequestSizeLimit(MaxBytes)]
    public async Task<IActionResult> UploadAvatar(IFormFile file, CancellationToken ct)
    {
        var url = await sender.Send(new UploadAvatarCommand(await ReadFileAsync(file)), ct);
        return Ok(new { url });
    }

    [HttpPost("coffee-image")]
    [Authorize(Policy = "Admin")]
    [RequestSizeLimit(MaxBytes)]
    public async Task<IActionResult> UploadCoffeeImage(IFormFile file, CancellationToken ct)
    {
        var url = await sender.Send(new UploadCoffeeImageCommand(await ReadFileAsync(file)), ct);
        return Ok(new { url });
    }

    private static async Task<UploadedFile> ReadFileAsync(IFormFile file)
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        return new UploadedFile(stream.ToArray(), file.FileName, file.ContentType);
    }
}
