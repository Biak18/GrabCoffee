using GrabCoffee.Application.Abstractions;
using MediatR;

namespace GrabCoffee.Application.Features.Promotions.GetActivePromotions;

// Mirrors fetchActivePromotions: codeless + live today. "Today" is UTC date
// server-side (mobile used device-local date; sub-day skew near midnight
// Myanmar time is accepted in favor of a single server clock).
public sealed record GetActivePromotionsQuery(Guid? StoreId = null)
    : IRequest<IReadOnlyList<PromotionDto>>;

public sealed class GetActivePromotionsHandler(IAppDbContext db)
    : IRequestHandler<GetActivePromotionsQuery, IReadOnlyList<PromotionDto>>
{
    public async Task<IReadOnlyList<PromotionDto>> Handle(GetActivePromotionsQuery request, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var promotions = await db.GetActivePromotionsAsync(request.StoreId, today, ct);
        return promotions.Select(PromotionMapper.ToDto).ToList();
    }
}
