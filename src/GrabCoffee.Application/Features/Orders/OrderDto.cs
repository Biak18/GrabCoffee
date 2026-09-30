namespace GrabCoffee.Application.Features.Orders;

public sealed record OrderSummaryDto(
    Guid Id,
    string Status,
    decimal Total,
    DateTime? PlacedAt,
    int ItemCount,
    string? ThumbnailUrl);

public sealed record OrderItemDto(
    Guid Id,
    Guid? CoffeeId,
    string? CoffeeName,
    string? CoffeeImageUrl,
    string? Size,
    string? Temperature,
    string? Milk,
    string[]? Extras,
    int Quantity,
    decimal UnitPrice,
    decimal? CompareAtPrice);

public sealed record OrderDetailsDto(
    Guid Id,
    Guid UserId,
    Guid StoreId,
    string Status,
    string Fulfillment,
    decimal Subtotal,
    decimal Tax,
    decimal Total,
    decimal Discount,
    decimal Tip,
    string? PromoCode,
    decimal DeliveryFee,
    string? DeliveryAddress,
    double? DeliveryLat,
    double? DeliveryLng,
    DateTime? PlacedAt,
    DateTime? ReadyAt,
    DateTime? CompletedAt,
    DateTime? CancelledAt,
    string PaymentMethod,
    string PaymentStatus,
    string? PaymentRef,
    Guid? DriverId,
    string? DriverName,
    string? DriverPhone,
    IReadOnlyList<OrderItemDto> Items);
