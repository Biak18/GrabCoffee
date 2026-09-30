using FluentValidation;
using GrabCoffee.Application.Abstractions;
using MediatR;

namespace GrabCoffee.Application.Features.Stores.OnboardSeller;

// Port of become_seller(): creates the store and flips profiles.role to
// seller in one transaction. Second call fails with "already have a store".
public sealed record OnboardSellerCommand(
    string StoreName,
    string StoreAddress,
    StoreHoursDto? Hours) : IRequest<Guid>;

public sealed class OnboardSellerValidator : AbstractValidator<OnboardSellerCommand>
{
    public OnboardSellerValidator()
    {
        RuleFor(x => x.StoreName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StoreAddress).NotEmpty().MaximumLength(500);
    }
}

public sealed class OnboardSellerHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<OnboardSellerCommand, Guid>
{
    public Task<Guid> Handle(OnboardSellerCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        return db.OnboardSellerAsync(
            userId,
            request.StoreName.Trim(),
            request.StoreAddress.Trim(),
            StoreMapper.WriteHours(request.Hours),
            ct);
    }
}
