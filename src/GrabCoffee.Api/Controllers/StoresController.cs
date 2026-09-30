using GrabCoffee.Application.Features.Stores;
using GrabCoffee.Application.Features.Orders.GetShopOrders;
using GrabCoffee.Application.Features.Stores.CreateCategory;
using GrabCoffee.Application.Features.Stores.CreateCoffee;
using GrabCoffee.Application.Features.Stores.CreateOption;
using GrabCoffee.Application.Features.Stores.CreatePromotion;
using GrabCoffee.Application.Features.Stores.DeleteOption;
using GrabCoffee.Application.Features.Stores.DeletePromotion;
using GrabCoffee.Application.Features.Stores.GetMyOptions;
using GrabCoffee.Application.Features.Stores.GetMyPromotions;
using GrabCoffee.Application.Features.Stores.GetMyStore;
using GrabCoffee.Application.Features.Stores.GetMyStoreCoffees;
using GrabCoffee.Application.Features.Stores.GetStoreById;
using GrabCoffee.Application.Features.Stores.GetStores;
using GrabCoffee.Application.Features.Stores.OnboardSeller;
using GrabCoffee.Application.Features.Stores.SetOptionScoping;
using GrabCoffee.Application.Features.Stores.TogglePromotionActive;
using GrabCoffee.Application.Features.Stores.UpdateCoffee;
using GrabCoffee.Application.Features.Stores.ToggleCoffeeActive;
using GrabCoffee.Application.Features.Stores.UpdateMyStore;
using GrabCoffee.Application.Features.Stores.UpdateOption;
using GrabCoffee.Application.Features.Stores.UpdatePromotion;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrabCoffee.Api.Controllers;

[ApiController]
[Route("stores")]
public sealed class StoresController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetStores(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetStoresQuery(page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetStoreByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyStoreQuery(), ct);
        return Ok(result);
    }

    [HttpPatch("mine")]
    [Authorize]
    public async Task<IActionResult> UpdateMine(
        [FromBody] UpdateMyStoreRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new UpdateMyStoreCommand(
            request.Name,
            request.Address,
            request.Hours,
            request.KpayPhone,
            request.PaymentNote,
            request.ContactPhone,
            request.Lat,
            request.Lng), ct);
        return Ok(result);
    }

    [HttpPost("onboard")]
    [Authorize]
    public async Task<IActionResult> Onboard(
        [FromBody] OnboardSellerRequest request,
        CancellationToken ct)
    {
        var id = await sender.Send(new OnboardSellerCommand(
            request.StoreName,
            request.StoreAddress,
            request.Hours), ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    // ---- My-store menu management (seller) ----
    [HttpGet("mine/coffees")]
    [Authorize]
    public async Task<IActionResult> GetMyCoffees(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyStoreCoffeesQuery(), ct);
        return Ok(result);
    }

    [HttpPost("mine/categories")]
    [Authorize]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateCategoryRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new CreateCategoryCommand(request.Name), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("mine/coffees")]
    [Authorize]
    public async Task<IActionResult> CreateCoffee(
        [FromBody] CreateCoffeeRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new CreateCoffeeCommand(
            request.Name, request.Description, request.BasePrice,
            request.ImageUrl, request.CategoryId,
            request.IsFeatured, request.IsActive), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPatch("mine/coffees/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateCoffee(
        Guid id,
        [FromBody] UpdateCoffeeRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new UpdateCoffeeCommand(
            id, request.Name, request.Description, request.BasePrice,
            request.ImageUrl, request.CategoryId,
            request.IsFeatured, request.IsActive), ct);
        return Ok(result);
    }

    [HttpPatch("mine/coffees/{id:guid}/active")]
    [Authorize]
    public async Task<IActionResult> ToggleCoffeeActive(
        Guid id,
        [FromBody] ToggleCoffeeActiveRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new ToggleCoffeeActiveCommand(id, request.IsActive), ct);
        return Ok(result);
    }

    [HttpGet("mine/options")]
    [Authorize]
    public async Task<IActionResult> GetMyOptions(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyOptionsQuery(), ct);
        return Ok(result);
    }

    [HttpPost("mine/options")]
    [Authorize]
    public async Task<IActionResult> CreateOption(
        [FromBody] CreateOptionRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new CreateOptionCommand(
            request.Type, request.Label, request.PriceDelta), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPatch("mine/options/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateOption(
        Guid id,
        [FromBody] UpdateOptionRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new UpdateOptionCommand(
            id, request.Type, request.Label, request.PriceDelta), ct);
        return Ok(result);
    }

    [HttpDelete("mine/options/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteOption(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteOptionCommand(id), ct);
        return NoContent();
    }

    [HttpPut("mine/options/{id:guid}/scoping")]
    [Authorize]
    public async Task<IActionResult> SetOptionScoping(
        Guid id,
        [FromBody] SetOptionScopingRequest request,
        CancellationToken ct)
    {
        await sender.Send(new SetOptionScopingCommand(id, request.CategoryIds), ct);
        return NoContent();
    }

    // ---- My-store promotions (seller) ----
    [HttpGet("mine/promotions")]
    [Authorize]
    public async Task<IActionResult> GetMyPromotions(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyPromotionsQuery(), ct);
        return Ok(result);
    }

    [HttpPost("mine/promotions")]
    [Authorize]
    public async Task<IActionResult> CreatePromotion(
        [FromBody] CreatePromotionRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new CreatePromotionCommand(
            request.Title, request.Description, request.DiscountPercent,
            request.Scope, request.CategoryId, request.CoffeeId,
            request.StartsAt, request.EndsAt, request.IsActive, request.Code), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPatch("mine/promotions/{id:int}")]
    [Authorize]
    public async Task<IActionResult> UpdatePromotion(
        int id,
        [FromBody] UpdatePromotionRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new UpdatePromotionCommand(
            id, request.Title, request.Description, request.DiscountPercent,
            request.Scope, request.CategoryId, request.CoffeeId,
            request.StartsAt, request.EndsAt, request.IsActive, request.Code), ct);
        return Ok(result);
    }

    [HttpDelete("mine/promotions/{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeletePromotion(int id, CancellationToken ct)
    {
        await sender.Send(new DeletePromotionCommand(id), ct);
        return NoContent();
    }

    [HttpPatch("mine/promotions/{id:int}/active")]
    [Authorize]
    public async Task<IActionResult> TogglePromotionActive(
        int id,
        [FromBody] TogglePromotionActiveRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new TogglePromotionActiveCommand(id, request.IsActive), ct);
        return Ok(result);
    }

    // ---- My-store order queue (seller) ----
    [HttpGet("mine/orders")]
    [Authorize]
    public async Task<IActionResult> GetMyShopOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetShopOrdersQuery(page, pageSize), ct);
        return Ok(result);
    }

    public sealed record UpdateMyStoreRequest(
        string Name,
        string Address,
        StoreHoursDto? Hours,
        string? KpayPhone,
        string? PaymentNote,
        string? ContactPhone,
        double? Lat,
        double? Lng);

    public sealed record OnboardSellerRequest(
        string StoreName,
        string StoreAddress,
        StoreHoursDto? Hours);

    public sealed record CreateCategoryRequest(string Name);

    public sealed record CreateCoffeeRequest(
        string Name,
        string? Description,
        decimal BasePrice,
        string? ImageUrl,
        Guid? CategoryId,
        bool IsFeatured,
        bool IsActive);

    public sealed record UpdateCoffeeRequest(
        string? Name,
        string? Description,
        decimal? BasePrice,
        string? ImageUrl,
        Guid? CategoryId,
        bool? IsFeatured,
        bool? IsActive);

    public sealed record ToggleCoffeeActiveRequest(bool IsActive);

    public sealed record CreateOptionRequest(string Type, string Label, decimal PriceDelta);

    public sealed record UpdateOptionRequest(string? Type, string? Label, decimal? PriceDelta);

    public sealed record SetOptionScopingRequest(List<Guid> CategoryIds);

    public sealed record CreatePromotionRequest(
        string Title,
        string Description,
        decimal DiscountPercent,
        string Scope,
        Guid? CategoryId,
        Guid? CoffeeId,
        DateOnly StartsAt,
        DateOnly EndsAt,
        bool IsActive,
        string? Code);

    public sealed record UpdatePromotionRequest(
        string? Title,
        string? Description,
        decimal? DiscountPercent,
        string? Scope,
        Guid? CategoryId,
        Guid? CoffeeId,
        DateOnly? StartsAt,
        DateOnly? EndsAt,
        bool? IsActive,
        string? Code);

    public sealed record TogglePromotionActiveRequest(bool IsActive);
}
