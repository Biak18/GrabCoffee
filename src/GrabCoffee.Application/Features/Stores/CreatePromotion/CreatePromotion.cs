using FluentValidation;
using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Features.Promotions;
using GrabCoffee.Domain.Entities;
using MediatR;

namespace GrabCoffee.Application.Features.Stores.CreatePromotion;

// Mirrors createPromotion. Scoped targets must belong to the seller's store:
// category scope needs CategoryId, coffee scope needs CoffeeId.
public sealed record CreatePromotionCommand(
    string Title,
    string Description,
    decimal DiscountPercent,
    string Scope,
    Guid? CategoryId,
    Guid? CoffeeId,
    DateOnly StartsAt,
    DateOnly EndsAt,
    bool IsActive,
    string? Code) : IRequest<PromotionDto>;

public sealed class CreatePromotionValidator : AbstractValidator<CreatePromotionCommand>
{
    private static readonly string[] AllowedScopes = ["all", "category", "coffee"];

    public CreatePromotionValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.DiscountPercent).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x.Scope)
            .Must(s => AllowedScopes.Contains(s.ToLowerInvariant()))
            .WithMessage("Scope must be one of: all, category, coffee.");
        RuleFor(x => x.CategoryId)
            .NotNull().When(x => x.Scope.Equals("category", StringComparison.OrdinalIgnoreCase))
            .WithMessage("CategoryId is required for category scope.");
        RuleFor(x => x.CoffeeId)
            .NotNull().When(x => x.Scope.Equals("coffee", StringComparison.OrdinalIgnoreCase))
            .WithMessage("CoffeeId is required for coffee scope.");
        RuleFor(x => x.EndsAt).GreaterThanOrEqualTo(x => x.StartsAt);
        RuleFor(x => x.Code).MaximumLength(100);
    }
}

public sealed class CreatePromotionHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<CreatePromotionCommand, PromotionDto>
{
    public async Task<PromotionDto> Handle(CreatePromotionCommand request, CancellationToken ct)
    {
        var store = await StoreAccess.RequireMyStoreAsync(db, currentUser, ct);
        await StoreAccess.RequireOwnTargetAsync(db, store.Id, request.CategoryId, request.CoffeeId, ct);

        var promotion = new Promotion
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            DiscountPercent = request.DiscountPercent,
            Scope = request.Scope.Trim().ToLowerInvariant(),
            CategoryId = request.CategoryId,
            CoffeeId = request.CoffeeId,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            IsActive = request.IsActive,
            StoreId = store.Id,
            Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim(),
        };
        await db.AddPromotionAsync(promotion, ct);
        await db.SaveChangesAsync(ct);
        return PromotionMapper.ToDto(promotion);
    }
}
