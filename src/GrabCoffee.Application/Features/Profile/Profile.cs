using FluentValidation;
using GrabCoffee.Application.Abstractions;
using MediatR;

namespace GrabCoffee.Application.Features.Profile;

// Own profile: display name + avatar URL (avatar bytes go through
// POST /uploads/avatar first, which returns the Cloudinary URL).
public sealed record UpdateProfileCommand(string? FullName, string? AvatarUrl) : IRequest;

public sealed record DeleteAccountCommand : IRequest;

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.FullName).MaximumLength(200);
        RuleFor(x => x.AvatarUrl).MaximumLength(1000);
    }
}

public sealed class UpdateProfileHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<UpdateProfileCommand>
{
    public async Task Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        await db.UpdateProfileAsync(
            userId,
            string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim(),
            string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim(),
            ct);
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
