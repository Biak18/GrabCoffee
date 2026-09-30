using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Features.Coffees;
using MediatR;

namespace GrabCoffee.Application.Features.Stores.GetMyStoreCoffees;

// Mirrors fetchMyCoffees: full list incl. inactive, ordered by name.
public sealed record GetMyStoreCoffeesQuery : IRequest<IReadOnlyList<CoffeeDto>>;

public sealed class GetMyStoreCoffeesHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetMyStoreCoffeesQuery, IReadOnlyList<CoffeeDto>>
{
    public async Task<IReadOnlyList<CoffeeDto>> Handle(GetMyStoreCoffeesQuery request, CancellationToken ct)
    {
        var store = await StoreAccess.RequireMyStoreAsync(db, currentUser, ct);
        var coffees = await db.GetCoffeesByStoreAsync(store.Id, ct);
        return coffees.Select(c => CoffeeMapper.ToDto(c, store.Name)).ToList();
    }
}
