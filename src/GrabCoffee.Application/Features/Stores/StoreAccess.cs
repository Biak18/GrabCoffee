using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Exceptions;
using GrabCoffee.Domain.Entities;

namespace GrabCoffee.Application.Features.Stores;

// Ownership is always resolved server-side from the JWT sub claim.
// Callers can never address another seller's store.
internal static class StoreAccess
{
    public static async Task<Store> RequireMyStoreAsync(
        IAppDbContext db, ICurrentUser currentUser, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");
        return await db.FindStoreByOwnerAsync(userId, ct)
            ?? throw new NotFoundException("No store found for this account.");
    }

    // Scoped promo/menu targets must belong to the seller's own store.
    public static async Task RequireOwnTargetAsync(
        IAppDbContext db, Guid storeId,
        Guid? categoryId, Guid? coffeeId, CancellationToken ct)
    {
        if (categoryId.HasValue)
        {
            var category = await db.FindCategoryAsync(categoryId.Value, ct);
            if (category is null || category.StoreId != storeId)
                throw new InvalidOperationException("Category does not belong to your store.");
        }
        if (coffeeId.HasValue)
        {
            var coffee = await db.FindCoffeeAsync(coffeeId.Value, ct);
            if (coffee is null || coffee.StoreId != storeId)
                throw new InvalidOperationException("Coffee does not belong to your store.");
        }
    }
}
