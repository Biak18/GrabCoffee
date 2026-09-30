using GrabCoffee.Application.Abstractions;
using MediatR;

namespace GrabCoffee.Application.Features.Categories.GetStoreCategories;

// Mirrors fetchCategoriesForStore (ordered by sort_order).
public sealed record GetStoreCategoriesQuery(Guid StoreId) : IRequest<IReadOnlyList<CategoryDto>>;

public sealed class GetStoreCategoriesHandler(IAppDbContext db)
    : IRequestHandler<GetStoreCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<IReadOnlyList<CategoryDto>> Handle(GetStoreCategoriesQuery request, CancellationToken ct)
    {
        var categories = await db.GetCategoriesByStoreAsync(request.StoreId, ct);
        return categories.Select(CategoryMapper.ToDto).ToList();
    }
}
