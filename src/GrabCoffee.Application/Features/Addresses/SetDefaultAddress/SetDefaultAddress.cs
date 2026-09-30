using GrabCoffee.Application.Abstractions;
using MediatR;

namespace GrabCoffee.Application.Features.Addresses.SetDefaultAddress;

// Port of set_default_address(): ownership check + atomic flip.
public sealed record SetDefaultAddressCommand(Guid Id) : IRequest;

public sealed class SetDefaultAddressHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<SetDefaultAddressCommand>
{
    public async Task Handle(SetDefaultAddressCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        await db.SetDefaultAddressAsync(userId, request.Id, ct);
    }
}
