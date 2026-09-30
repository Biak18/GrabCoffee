using GrabCoffee.Application.Abstractions;
using MediatR;

namespace GrabCoffee.Application.Features.Coffees.GetHighlights;

// One round trip for the home screen (mobile calls featured + popular +
// recommended separately today).
public sealed record GetCoffeeHighlightsQuery : IRequest<CoffeeHighlightsDto>;

public sealed class GetCoffeeHighlightsHandler(IAppDbContext db)
    : IRequestHandler<GetCoffeeHighlightsQuery, CoffeeHighlightsDto>
{
    public async Task<CoffeeHighlightsDto> Handle(GetCoffeeHighlightsQuery request, CancellationToken ct)
    {
        var featured = await db.GetFeaturedCoffeesAsync(ct);
        var popular = await db.GetPopularCoffeesAsync(ct);
        var recommended = await db.GetRecommendedCoffeesAsync(ct);

        var all = featured.Concat(popular).Concat(recommended).ToList();
        var names = await db.GetStoreNamesAsync(all.Select(c => c.StoreId).Distinct(), ct);

        return new CoffeeHighlightsDto(
            featured.Select(c => CoffeeMapper.ToDto(c, names.GetValueOrDefault(c.StoreId))).ToList(),
            popular.Select(c => CoffeeMapper.ToDto(c, names.GetValueOrDefault(c.StoreId))).ToList(),
            recommended.Select(c => CoffeeMapper.ToDto(c, names.GetValueOrDefault(c.StoreId))).ToList());
    }
}
