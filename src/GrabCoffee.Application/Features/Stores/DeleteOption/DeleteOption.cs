using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Exceptions;
using MediatR;

namespace GrabCoffee.Application.Features.Stores.DeleteOption;

// Mirrors deleteOption (scoping rows go with it).
public sealed record DeleteOptionCommand(Guid Id) : IRequest;

public sealed class DeleteOptionHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<DeleteOptionCommand>
{
    public async Task Handle(DeleteOptionCommand request, CancellationToken ct)
    {
        var store = await StoreAccess.RequireMyStoreAsync(db, currentUser, ct);

        var option = await db.FindOptionAsync(request.Id, ct);
        if (option is null || option.StoreId != store.Id)
            throw new NotFoundException($"Option {request.Id} not found.");

        await db.RemoveOptionAsync(option, ct);
        await db.SaveChangesAsync(ct);
    }
}
