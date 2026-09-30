using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Exceptions;
using MediatR;

namespace GrabCoffee.Application.Features.Stores.DeletePromotion;

// Mirrors deletePromotion.
public sealed record DeletePromotionCommand(int Id) : IRequest;

public sealed class DeletePromotionHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<DeletePromotionCommand>
{
    public async Task Handle(DeletePromotionCommand request, CancellationToken ct)
    {
        var store = await StoreAccess.RequireMyStoreAsync(db, currentUser, ct);

        var promotion = await db.FindPromotionAsync(request.Id, ct);
        if (promotion is null || promotion.StoreId != store.Id)
            throw new NotFoundException($"Promotion {request.Id} not found.");

        await db.RemovePromotionAsync(promotion, ct);
        await db.SaveChangesAsync(ct);
    }
}
