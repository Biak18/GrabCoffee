using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Exceptions;
using MediatR;

namespace GrabCoffee.Application.Features.Stores.GetStoreById;

public sealed record GetStoreByIdQuery(Guid Id) : IRequest<StoreDto>;

public sealed class GetStoreByIdHandler(IAppDbContext db) : IRequestHandler<GetStoreByIdQuery, StoreDto>
{
    public async Task<StoreDto> Handle(GetStoreByIdQuery request, CancellationToken ct)
    {
        var store = await db.FindStoreAsync(request.Id, ct)
            ?? throw new NotFoundException($"Store {request.Id} not found.");
        return StoreMapper.ToDto(store);
    }
}
