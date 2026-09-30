using GrabCoffee.Application.Abstractions;
using MediatR;

namespace GrabCoffee.Application.Features.Auth.Logout;

public sealed record LogoutCommand(string AccessToken) : IRequest;

public sealed class LogoutHandler(IAuthClient auth) : IRequestHandler<LogoutCommand>
{
    public Task Handle(LogoutCommand request, CancellationToken cancellationToken)
        => auth.LogoutAsync(request.AccessToken, cancellationToken);
}
