using FluentValidation;
using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Exceptions;
using GrabCoffee.Application.Features.Promotions;
using MediatR;

namespace GrabCoffee.Application.Features.Stores.UpdatePromotion;

// Mirrors updatePromotion(id, partial).
public sealed record UpdatePromotionCommand(
    int Id,
    string? Title,
    string? Description,
    decimal? DiscountPercent,
    string? Scope,
    Guid? CategoryId,
    Guid? CoffeeId,
    DateOnly? StartsAt,
    DateOnly? EndsAt,
    bool? IsActive,
    string? Code) : IRequest<PromotionDto>;

public sealed class UpdatePromotionValidator : AbstractValidator<UpdatePromotionCommand>
{
    private static readonly string[] AllowedScopes = ["all", "category", "coffee"];

    public UpdatePromotionValidator()
    {
        RuleFor(x => x.Title).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.DiscountPercent)
            .GreaterThan(0).LessThanOrEqualTo(100)
            .When(x => x.DiscountPercent.HasValue);
        RuleFor(x => x.Scope)
            .Must(s => s is null || AllowedScopes.Contains(s.ToLowerInvariant()))
            .WithMessage("Scope must be one of: all, category, coffee.");
        RuleFor(x => x.Code).MaximumLength(100);
    }
}

public sealed class UpdatePromotionHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<UpdatePromotionCommand, PromotionDto>
{
    public async Task<PromotionDto> Handle(UpdatePromotionCommand request, CancellationToken ct)
    {
        var store = await StoreAccess.RequireMyStoreAsync(db, currentUser, ct);

        var promotion = await db.FindPromotionAsync(request.Id, ct);
        if (promotion is null || promotion.StoreId != store.Id)
            throw new NotFoundException($"Promotion {request.Id} not found.");

        var scope = request.Scope ?? promotion.Scope;
        var categoryId = request.CategoryId ?? promotion.CategoryId;
        var coffeeId = request.CoffeeId ?? promotion.CoffeeId;
        await StoreAccess.RequireOwnTargetAsync(db, store.Id, categoryId, coffeeId, ct);

        if (request.Title is not null)
            promotion.Title = request.Title.Trim();
        if (request.Description is not null)
            promotion.Description = request.Description.Trim();
        if (request.DiscountPercent.HasValue)
            promotion.DiscountPercent = request.DiscountPercent.Value;
        if (request.Scope is not null)
            promotion.Scope = request.Scope.Trim().ToLowerInvariant();
        if (request.CategoryId is not null)
            promotion.CategoryId = request.CategoryId;
        if (request.CoffeeId is not null)
            promotion.CoffeeId = request.CoffeeId;
        if (request.StartsAt.HasValue)
            promotion.StartsAt = request.StartsAt.Value;
        if (request.EndsAt.HasValue)
            promotion.EndsAt = request.EndsAt.Value;
        if (request.IsActive.HasValue)
            promotion.IsActive = request.IsActive.Value;
        if (request.Code is not null)
            promotion.Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();

        if (promotion.EndsAt < promotion.StartsAt)
            throw new InvalidOperationException("EndsAt must be on or after StartsAt.");

        await db.SaveChangesAsync(ct);
        return PromotionMapper.ToDto(promotion);
    }
}
