using GrabCoffee.Application.Abstractions;
using MediatR;

namespace GrabCoffee.Application.Features.Stores.GetMyOptions;

// Mirrors fetchMyOptions (with per-option category scoping).
public sealed record GetMyOptionsQuery : IRequest<IReadOnlyList<SellerOptionDto>>;

public sealed class GetMyOptionsHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetMyOptionsQuery, IReadOnlyList<SellerOptionDto>>
{
    public async Task<IReadOnlyList<SellerOptionDto>> Handle(GetMyOptionsQuery request, CancellationToken ct)
    {
        var store = await StoreAccess.RequireMyStoreAsync(db, currentUser, ct);
        var options = await db.GetOptionsByStoreAsync(store.Id, ct);
        var scoping = await db.GetOptionCategoryMapAsync(store.Id, ct);

        return options.Select(o => new SellerOptionDto(
            o.Id, o.Type, o.Label, o.PriceDelta, o.StoreId,
            scoping.GetValueOrDefault(o.Id, []))).ToList();
    }
}
