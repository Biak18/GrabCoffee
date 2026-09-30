using FluentValidation;
using GrabCoffee.Application.Abstractions;
using MediatR;

namespace GrabCoffee.Application.Features.Promotions.LookupPromoCode;

// Mirrors lookupPromoCode: voucher lookup scoped to one store, live today,
// case-insensitive code match. Null when nothing live matches.
public sealed record LookupPromoCodeQuery(Guid StoreId, string Code)
    : IRequest<PromotionDto?>;

public sealed class LookupPromoCodeValidator : AbstractValidator<LookupPromoCodeQuery>
{
    public LookupPromoCodeValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100);
    }
}

public sealed class LookupPromoCodeHandler(IAppDbContext db)
    : IRequestHandler<LookupPromoCodeQuery, PromotionDto?>
{
    public async Task<PromotionDto?> Handle(LookupPromoCodeQuery request, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var promotion = await db.LookupPromoCodeAsync(request.StoreId, request.Code.Trim(), today, ct);
        return promotion is null ? null : PromotionMapper.ToDto(promotion);
    }
}
