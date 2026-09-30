using GrabCoffee.Domain.Entities;

namespace GrabCoffee.Application.Features.Categories;

public sealed record CategoryDto(Guid Id, string Name, int SortOrder, Guid StoreId);

internal static class CategoryMapper
{
    public static CategoryDto ToDto(Category category) => new(
        category.Id, category.Name, category.SortOrder, category.StoreId);
}
