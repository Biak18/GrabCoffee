using GrabCoffee.Application.Abstractions;
using MediatR;

namespace GrabCoffee.Application.Features.Addresses.GetAddresses;

// Mirrors fetchAddresses (default first, then newest).
public sealed record GetAddressesQuery : IRequest<IReadOnlyList<AddressDto>>;

public sealed class GetAddressesHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetAddressesQuery, IReadOnlyList<AddressDto>>
{
    public async Task<IReadOnlyList<AddressDto>> Handle(GetAddressesQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        var addresses = await db.GetAddressesAsync(userId, ct);
        return addresses.Select(AddressMapper.ToDto).ToList();
    }
}
