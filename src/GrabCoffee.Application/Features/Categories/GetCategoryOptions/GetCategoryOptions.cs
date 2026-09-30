using GrabCoffee.Application.Abstractions;
using GrabCoffee.Domain.Entities;
using MediatR;

namespace GrabCoffee.Application.Features.Categories.GetCategoryOptions;

// Mirrors get_coffee_options(): options of the category's store that are
// unscoped or explicitly scoped to this category.
public sealed record CoffeeOptionDto(
    Guid Id,
    string Type,
    string Label,
    decimal? PriceDelta,
    Guid StoreId);

public sealed record GetCategoryOptionsQuery(Guid CategoryId) : IRequest<IReadOnlyList<CoffeeOptionDto>>;

public sealed class GetCategoryOptionsHandler(IAppDbContext db)
    : IRequestHandler<GetCategoryOptionsQuery, IReadOnlyList<CoffeeOptionDto>>
{
    public async Task<IReadOnlyList<CoffeeOptionDto>> Handle(GetCategoryOptionsQuery request, CancellationToken ct)
    {
        var options = await db.GetOptionsForCategoryAsync(request.CategoryId, ct);
        return options.Select(ToDto).ToList();
    }

    private static CoffeeOptionDto ToDto(CoffeeOption o) => new(
        o.Id, o.Type, o.Label, o.PriceDelta, o.StoreId);
}
