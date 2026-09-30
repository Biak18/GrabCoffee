using GrabCoffee.Application.Abstractions;
using MediatR;

namespace GrabCoffee.Application.Features.Loyalty.GetMyCards;

// Mirrors fetchMyLoyaltyCards (stamps per store + store name).
public sealed record LoyaltyCardDto(Guid StoreId, string StoreName, int Stamps);

public sealed record GetMyCardsQuery : IRequest<IReadOnlyList<LoyaltyCardDto>>;

public sealed class GetMyCardsHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetMyCardsQuery, IReadOnlyList<LoyaltyCardDto>>
{
    public async Task<IReadOnlyList<LoyaltyCardDto>> Handle(GetMyCardsQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        var cards = await db.GetLoyaltyCardsAsync(userId, ct);
        var names = await db.GetStoreNamesAsync(cards.Select(c => c.StoreId).Distinct(), ct);
        return cards.Select(c => new LoyaltyCardDto(
            c.StoreId, names.GetValueOrDefault(c.StoreId, "Store"), c.Stamps)).ToList();
    }
}
