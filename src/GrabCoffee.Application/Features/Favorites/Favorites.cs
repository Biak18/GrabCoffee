using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Features.Coffees;
using GrabCoffee.Application.Features.Stores;
using MediatR;

namespace GrabCoffee.Application.Features.Favorites;

// Coffee + store favorites (toggle endpoints are idempotent no-ops when the
// row already is / isn't there — mobile toggles from local state).
public sealed record GetFavoriteCoffeesQuery : IRequest<IReadOnlyList<CoffeeDto>>;
public sealed record ToggleCoffeeFavoriteCommand(Guid CoffeeId, bool Liked) : IRequest;
public sealed record GetFavoriteStoresQuery : IRequest<IReadOnlyList<StoreDto>>;
public sealed record ToggleStoreFavoriteCommand(Guid StoreId, bool Liked) : IRequest;

public sealed class GetFavoriteCoffeesHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetFavoriteCoffeesQuery, IReadOnlyList<CoffeeDto>>
{
    public async Task<IReadOnlyList<CoffeeDto>> Handle(GetFavoriteCoffeesQuery request, CancellationToken ct)
    {
        var userId = FavoritesAuth.RequireUser(currentUser);
        var coffees = await db.GetFavoriteCoffeesAsync(userId, ct);
        return await CoffeeMapper.ToDtosAsync(db, coffees, ct);
    }
}

public sealed class ToggleCoffeeFavoriteHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<ToggleCoffeeFavoriteCommand>
{
    public async Task Handle(ToggleCoffeeFavoriteCommand request, CancellationToken ct)
    {
        var userId = FavoritesAuth.RequireUser(currentUser);
        if (request.Liked)
            await db.AddFavoriteAsync(userId, request.CoffeeId, ct);
        else
            await db.RemoveFavoriteAsync(userId, request.CoffeeId, ct);
        await db.SaveChangesAsync(ct);
    }
}

public sealed class GetFavoriteStoresHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetFavoriteStoresQuery, IReadOnlyList<StoreDto>>
{
    public async Task<IReadOnlyList<StoreDto>> Handle(GetFavoriteStoresQuery request, CancellationToken ct)
    {
        var userId = FavoritesAuth.RequireUser(currentUser);
        var stores = await db.GetFavoriteStoresAsync(userId, ct);
        return stores.Select(StoreMapper.ToDto).ToList();
    }
}

public sealed class ToggleStoreFavoriteHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<ToggleStoreFavoriteCommand>
{
    public async Task Handle(ToggleStoreFavoriteCommand request, CancellationToken ct)
    {
        var userId = FavoritesAuth.RequireUser(currentUser);
        if (request.Liked)
            await db.AddStoreFavoriteAsync(userId, request.StoreId, ct);
        else
            await db.RemoveStoreFavoriteAsync(userId, request.StoreId, ct);
        await db.SaveChangesAsync(ct);
    }
}

file static class FavoritesAuth
{
    public static Guid RequireUser(ICurrentUser currentUser)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");
        return userId;
    }
}
