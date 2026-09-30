using GrabCoffee.Application.Features.Search;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrabCoffee.Api.Controllers;

[ApiController]
[Route("search")]
public sealed class SearchController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] string term, CancellationToken ct)
    {
        var result = await sender.Send(new SearchQuery(term), ct);
        return Ok(result);
    }
}
