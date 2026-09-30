using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Features.Promotions;
using MediatR;

namespace GrabCoffee.Application.Features.Stores.GetMyPromotions;

// Mirrors fetchMyPromotions (all promos of my store, newest first).
public sealed record GetMyPromotionsQuery : IRequest<IReadOnlyList<PromotionDto>>;

public sealed class GetMyPromotionsHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetMyPromotionsQuery, IReadOnlyList<PromotionDto>>
{
    public async Task<IReadOnlyList<PromotionDto>> Handle(GetMyPromotionsQuery request, CancellationToken ct)
    {
        var store = await StoreAccess.RequireMyStoreAsync(db, currentUser, ct);
        var promotions = await db.GetPromotionsByStoreAsync(store.Id, ct);
        return promotions.Select(PromotionMapper.ToDto).ToList();
    }
}
