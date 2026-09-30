using FluentValidation;
using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Features.Orders.Pricing;
using MediatR;

namespace GrabCoffee.Application.Features.Orders.PriceCheck;

// Port of expected_cart_prices(): per-line server pricing. A line that can
// no longer be priced (removed/deactivated coffee, invalid option) resolves
// to nulls instead of failing the whole check.
public sealed record PriceCheckLineInput(
    Guid CoffeeId,
    string? Size,
    string? Temperature,
    string? Milk,
    string[]? Extras,
    int Quantity);

public sealed record PriceCheckQuery(Guid StoreId, List<PriceCheckLineInput> Items)
    : IRequest<IReadOnlyList<LinePriceDto>>;

public sealed record LinePriceDto(Guid CoffeeId, decimal? UnitPrice, decimal? FullPrice);

public sealed class PriceCheckValidator : AbstractValidator<PriceCheckQuery>
{
    public PriceCheckValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(line =>
        {
            line.RuleFor(l => l.CoffeeId).NotEmpty();
            line.RuleFor(l => l.Quantity).InclusiveBetween(1, 99);
        });
    }
}

public sealed class PriceCheckHandler(IAppDbContext db) : IRequestHandler<PriceCheckQuery, IReadOnlyList<LinePriceDto>>
{
    public async Task<IReadOnlyList<LinePriceDto>> Handle(PriceCheckQuery request, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var coffees = (await db.FindActiveCoffeesAsync(
                request.StoreId, request.Items.Select(i => i.CoffeeId).Distinct(), ct))
            .ToDictionary(c => c.Id);
        var options = await db.GetStoreOptionsAsync(request.StoreId, ct);
        var autoPromos = await db.GetActivePromotionsAsync(request.StoreId, today, ct);

        var result = new List<LinePriceDto>(request.Items.Count);
        foreach (var line in request.Items)
        {
            try
            {
                if (!coffees.TryGetValue(line.CoffeeId, out var coffee))
                    throw new InvalidOperationException("Coffee is unavailable for this store.");

                var promo = OrderPricing.BestAutomaticPromo(autoPromos, coffee.Id, coffee.CategoryId);
                var priced = OrderPricing.PriceLine(
                    coffee, options, promo,
                    line.Size, line.Temperature, line.Milk, line.Extras ?? []);
                result.Add(new LinePriceDto(line.CoffeeId, priced.UnitPrice, priced.FullPrice));
            }
            catch (Exception)
            {
                // Mirrors `exception when others then nulls` per line.
                result.Add(new LinePriceDto(line.CoffeeId, null, null));
            }
        }
        return result;
    }
}
