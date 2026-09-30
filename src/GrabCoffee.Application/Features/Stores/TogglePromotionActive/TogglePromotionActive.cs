using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Exceptions;
using GrabCoffee.Application.Features.Promotions;
using MediatR;

namespace GrabCoffee.Application.Features.Stores.TogglePromotionActive;

// Mirrors togglePromotionActive.
public sealed record TogglePromotionActiveCommand(int Id, bool IsActive)
    : IRequest<PromotionDto>;

public sealed class TogglePromotionActiveHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<TogglePromotionActiveCommand, PromotionDto>
{
    public async Task<PromotionDto> Handle(TogglePromotionActiveCommand request, CancellationToken ct)
    {
        var store = await StoreAccess.RequireMyStoreAsync(db, currentUser, ct);

        var promotion = await db.FindPromotionAsync(request.Id, ct);
        if (promotion is null || promotion.StoreId != store.Id)
            throw new NotFoundException($"Promotion {request.Id} not found.");

        promotion.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
        return PromotionMapper.ToDto(promotion);
    }
}
