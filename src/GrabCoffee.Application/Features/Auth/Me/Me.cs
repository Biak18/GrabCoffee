using GrabCoffee.Application.Abstractions;
using MediatR;

namespace GrabCoffee.Application.Features.Auth.Me;

public sealed record MeResponse(Guid Id, string? Email, string? FullName, string Role);

public sealed record MeQuery : IRequest<MeResponse>;

public sealed class MeHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<MeQuery, MeResponse>
{
    public async Task<MeResponse> Handle(MeQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        var profile = await db.FindProfileAsync(userId, cancellationToken);
        return new MeResponse(userId, currentUser.Email, profile?.FullName, profile?.Role ?? "customer");
    }
}
