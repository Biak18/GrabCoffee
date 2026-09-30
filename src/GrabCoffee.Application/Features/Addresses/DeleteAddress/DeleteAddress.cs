using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Exceptions;
using MediatR;

namespace GrabCoffee.Application.Features.Addresses.DeleteAddress;

// Mirrors deleteAddress (ownership enforced).
public sealed record DeleteAddressCommand(Guid Id) : IRequest;

public sealed class DeleteAddressHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<DeleteAddressCommand>
{
    public async Task Handle(DeleteAddressCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        var address = await db.FindAddressAsync(request.Id, ct);
        if (address is null || address.UserId != userId)
            throw new NotFoundException("Address not found.");

        await db.RemoveAddressAsync(address, ct);
        await db.SaveChangesAsync(ct);
    }
}
