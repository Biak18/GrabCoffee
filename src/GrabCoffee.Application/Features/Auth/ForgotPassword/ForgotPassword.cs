using GrabCoffee.Application.Abstractions;
using MediatR;

namespace GrabCoffee.Application.Features.Auth.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest;

public sealed class ForgotPasswordHandler(IAuthClient auth) : IRequestHandler<ForgotPasswordCommand>
{
    public Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        // Always silent — never reveal if email exists.
        if (string.IsNullOrWhiteSpace(request.Email))
            return Task.CompletedTask;
        return auth.RequestPasswordResetAsync(request.Email, cancellationToken);
    }
}
