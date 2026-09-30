namespace GrabCoffee.Application.Features.Stores;

public sealed record SellerOptionDto(
    Guid Id,
    string Type,
    string Label,
    decimal? PriceDelta,
    Guid StoreId,
    IReadOnlyList<Guid> CategoryIds);
