using GrabCoffee.Application.Features.Categories.GetCategoryOptions;
using GrabCoffee.Application.Features.Categories.GetStoreCategories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrabCoffee.Api.Controllers;

[ApiController]
[Route("categories")]
public sealed class CategoriesController(ISender sender) : ControllerBase
{
    [HttpGet("/stores/{storeId:guid}/categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByStore(Guid storeId, CancellationToken ct)
    {
        var result = await sender.Send(new GetStoreCategoriesQuery(storeId), ct);
        return Ok(result);
    }

    [HttpGet("{categoryId:guid}/options")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOptions(Guid categoryId, CancellationToken ct)
    {
        var result = await sender.Send(new GetCategoryOptionsQuery(categoryId), ct);
        return Ok(result);
    }
}
