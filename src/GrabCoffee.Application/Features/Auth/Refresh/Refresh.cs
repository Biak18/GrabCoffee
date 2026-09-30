using FluentValidation;
using GrabCoffee.Application.Abstractions;
using MediatR;

namespace GrabCoffee.Application.Features.Auth.Refresh;

public sealed record RefreshCommand(string RefreshToken) : IRequest<AuthResult>;

public sealed class RefreshValidator : AbstractValidator<RefreshCommand>
{
    public RefreshValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

public sealed class RefreshHandler(IAuthClient auth) : IRequestHandler<RefreshCommand, AuthResult>
{
    public Task<AuthResult> Handle(RefreshCommand request, CancellationToken cancellationToken)
        => auth.RefreshAsync(request.RefreshToken, cancellationToken);
}
