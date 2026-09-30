using FluentValidation;
using GrabCoffee.Application.Abstractions;
using MediatR;

namespace GrabCoffee.Application.Features.Auth.ChangePassword;

// Mirrors changePassword (GoTrue updateUser with the caller's own token).
public sealed record ChangePasswordCommand(string AccessToken, string NewPassword) : IRequest;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.AccessToken).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6);
    }
}

public sealed class ChangePasswordHandler(IAuthClient auth) : IRequestHandler<ChangePasswordCommand>
{
    public Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        => auth.UpdatePasswordAsync(request.AccessToken, request.NewPassword, cancellationToken);
}
