using FluentValidation;
using GrabCoffee.Application.Abstractions;
using MediatR;

namespace GrabCoffee.Application.Features.Auth.Register;

public sealed record RegisterCommand(string Email, string Password, string? FullName) : IRequest<RegisterResult>;

public sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public sealed class RegisterHandler(IAuthClient auth) : IRequestHandler<RegisterCommand, RegisterResult>
{
    public Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
        => auth.RegisterAsync(request.Email, request.Password, request.FullName, cancellationToken);
}
