using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Exceptions;
using MediatR;

namespace GrabCoffee.Application.Features.Stores.GetMyStore;

public sealed record GetMyStoreQuery : IRequest<StoreDto>;

public sealed class GetMyStoreHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetMyStoreQuery, StoreDto>
{
    public async Task<StoreDto> Handle(GetMyStoreQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        var store = await db.FindStoreByOwnerAsync(userId, ct)
            ?? throw new NotFoundException("No store found for this account.");
        return StoreMapper.ToDto(store);
    }
}
