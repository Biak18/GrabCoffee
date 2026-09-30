using FluentValidation;
using GrabCoffee.Application.Abstractions;
using MediatR;

namespace GrabCoffee.Application.Features.Profile;

// Mirrors updateDisplayName + deleteAccount (delete_account RPC).
// Avatar upload stays direct-to-Storage on mobile, unchanged.
public sealed record UpdateDisplayNameCommand(string FullName) : IRequest;

public sealed record DeleteAccountCommand : IRequest;

public sealed class UpdateDisplayNameValidator : AbstractValidator<UpdateDisplayNameCommand>
{
    public UpdateDisplayNameValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
    }
}

public sealed class UpdateDisplayNameHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<UpdateDisplayNameCommand>
{
    public async Task Handle(UpdateDisplayNameCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        await db.UpdateDisplayNameAsync(userId, request.FullName.Trim(), ct);
    }
}

public sealed class DeleteAccountHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<DeleteAccountCommand>
{
    public async Task Handle(DeleteAccountCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        await db.DeleteAccountAsync(userId, ct);
    }
}
