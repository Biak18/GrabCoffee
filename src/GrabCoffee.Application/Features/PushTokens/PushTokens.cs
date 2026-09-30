using FluentValidation;
using GrabCoffee.Application.Abstractions;
using MediatR;

namespace GrabCoffee.Application.Features.PushTokens;

// Mirrors usePushNotifications: upsert {user_id, token} on conflict token;
// sign-out/disable removes the caller's tokens.
public sealed record RegisterPushTokenCommand(string Token) : IRequest;

public sealed record UnregisterPushTokensCommand : IRequest;

public sealed class RegisterPushTokenValidator : AbstractValidator<RegisterPushTokenCommand>
{
    public RegisterPushTokenValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(500);
    }
}

public sealed class RegisterPushTokenHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<RegisterPushTokenCommand>
{
    public async Task Handle(RegisterPushTokenCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        await db.UpsertPushTokenAsync(userId, request.Token.Trim(), ct);
    }
}

public sealed class UnregisterPushTokensHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<UnregisterPushTokensCommand>
{
    public async Task Handle(UnregisterPushTokensCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        await db.DeleteMyPushTokensAsync(userId, ct);
    }
}
