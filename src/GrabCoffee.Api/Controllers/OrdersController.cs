using GrabCoffee.Application.Features.Orders.AssignDriver;
using GrabCoffee.Application.Features.Orders.AttachPayment;
using GrabCoffee.Application.Features.Orders.CancelOrder;
using GrabCoffee.Application.Features.Orders.GetDeliveries;
using GrabCoffee.Application.Features.Orders.GetMyOrders;
using GrabCoffee.Application.Features.Orders.GetOrderById;
using GrabCoffee.Application.Features.Orders.PlaceOrder;
using GrabCoffee.Application.Features.Orders.PriceCheck;
using GrabCoffee.Application.Features.Orders.UpdateOrderStatus;
using GrabCoffee.Application.Features.Orders.VerifyPayment;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrabCoffee.Api.Controllers;

[ApiController]
[Route("orders")]
public sealed class OrdersController(ISender sender) : ControllerBase
{
    [HttpPost("price-check")]
    [AllowAnonymous]
    public async Task<IActionResult> PriceCheck(
        [FromBody] PriceCheckRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new PriceCheckQuery(
            request.StoreId,
            request.Items.Select(i => new PriceCheckLineInput(
                i.CoffeeId, i.Size, i.Temperature, i.Milk, i.Extras, i.Quantity)).ToList()), ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> PlaceOrder(
        [FromBody] PlaceOrderRequest request,
        CancellationToken ct)
    {
        var id = await sender.Send(new PlaceOrderCommand(
            request.StoreId, request.Fulfillment,
            request.Items.Select(i => new PlaceOrderLineInput(
                i.CoffeeId, i.Size, i.Temperature, i.Milk, i.Extras, i.Quantity, i.UnitPrice)).ToList(),
            request.Tip, request.PromoCode, request.RedeemLoyalty, request.IdempotencyKey,
            request.DeliveryAddress, request.DeliveryLat, request.DeliveryLng), ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPost("{id:guid}/payment")]
    [Authorize]
    public async Task<IActionResult> AttachPayment(
        Guid id,
        [FromBody] AttachPaymentRequest request,
        CancellationToken ct)
    {
        await sender.Send(new AttachPaymentCommand(id, request.Method, request.Ref), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/payment/verify")]
    [Authorize]
    public async Task<IActionResult> VerifyPayment(
        Guid id,
        [FromBody] VerifyPaymentRequest request,
        CancellationToken ct)
    {
        await sender.Send(new VerifyPaymentCommand(id, request.Verified), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize]
    public async Task<IActionResult> CancelOrder(Guid id, CancellationToken ct)
    {
        await sender.Send(new CancelOrderCommand(id), ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateStatusRequest request,
        CancellationToken ct)
    {
        await sender.Send(new UpdateOrderStatusCommand(id, request.Status), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/assign-driver")]
    [Authorize]
    public async Task<IActionResult> AssignDriver(
        Guid id,
        [FromBody] AssignDriverRequest request,
        CancellationToken ct)
    {
        await sender.Send(new AssignDriverCommand(id, request.DriverId), ct);
        return NoContent();
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetMyOrdersQuery(page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("deliveries")]
    [Authorize]
    public async Task<IActionResult> GetDeliveries(CancellationToken ct)
    {
        var result = await sender.Send(new GetDeliveriesQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetOrderByIdQuery(id), ct);
        return Ok(result);
    }

    public sealed record PriceCheckLineRequest(
        Guid CoffeeId,
        string? Size,
        string? Temperature,
        string? Milk,
        string[]? Extras,
        int Quantity);

    public sealed record PriceCheckRequest(
        Guid StoreId,
        List<PriceCheckLineRequest> Items);

    public sealed record PlaceOrderLineRequest(
        Guid CoffeeId,
        string? Size,
        string? Temperature,
        string? Milk,
        string[]? Extras,
        int Quantity,
        decimal UnitPrice);

    public sealed record PlaceOrderRequest(
        Guid StoreId,
        string Fulfillment,
        List<PlaceOrderLineRequest> Items,
        decimal Tip,
        string? PromoCode,
        bool RedeemLoyalty,
        string? IdempotencyKey,
        string? DeliveryAddress,
        double? DeliveryLat,
        double? DeliveryLng);

    public sealed record AttachPaymentRequest(string Method, string Ref);
    public sealed record VerifyPaymentRequest(bool Verified);
    public sealed record UpdateStatusRequest(string Status);
    public sealed record AssignDriverRequest(Guid DriverId);
}
