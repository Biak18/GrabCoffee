using GrabCoffee.Domain.Entities;

namespace GrabCoffee.Application.Features.Promotions;

public sealed record PromotionDto(
    int Id,
    string Title,
    string Description,
    decimal DiscountPercent,
    string Scope,
    Guid? CategoryId,
    Guid? CoffeeId,
    DateOnly StartsAt,
    DateOnly EndsAt,
    bool IsActive,
    Guid StoreId,
    string? Code);

internal static class PromotionMapper
{
    public static PromotionDto ToDto(Promotion p) => new(
        p.Id, p.Title, p.Description, p.DiscountPercent, p.Scope,
        p.CategoryId, p.CoffeeId, p.StartsAt, p.EndsAt,
        p.IsActive, p.StoreId, p.Code);
}
