using GrabCoffee.Domain.Entities;

namespace GrabCoffee.Application.Features.Coffees;

public sealed record CoffeeDto(
    Guid Id,
    Guid? CategoryId,
    string Name,
    string? Description,
    decimal BasePrice,
    string? ImageUrl,
    decimal? Rating,
    bool IsFeatured,
    bool IsActive,
    Guid StoreId,
    string? StoreName);

public sealed record CoffeeHighlightsDto(
    IReadOnlyList<CoffeeDto> Featured,
    IReadOnlyList<CoffeeDto> Popular,
    IReadOnlyList<CoffeeDto> Recommended);

internal static class CoffeeMapper
{
    public static CoffeeDto ToDto(Coffee coffee, string? storeName) => new(
        coffee.Id,
        coffee.CategoryId,
        coffee.Name,
        coffee.Description,
        coffee.BasePrice,
        coffee.ImageUrl,
        coffee.Rating,
        coffee.IsFeatured,
        coffee.IsActive,
        coffee.StoreId,
        storeName);

    public static async Task<List<CoffeeDto>> ToDtosAsync(
        GrabCoffee.Application.Abstractions.IAppDbContext db,
        IEnumerable<Coffee> coffees,
        CancellationToken ct)
    {
        var list = coffees.ToList();
        var names = await db.GetStoreNamesAsync(list.Select(c => c.StoreId).Distinct(), ct);
        return list.Select(c => ToDto(c, names.GetValueOrDefault(c.StoreId))).ToList();
    }
}
