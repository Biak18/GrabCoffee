using GrabCoffee.Domain.Entities;

namespace GrabCoffee.Application.Features.Orders.Pricing;

// Pure port of item_price_parts() + the voucher/loyalty math from
// create_order_with_delivery_fee(). Throws InvalidOperationException with
// the exact server messages so mobile toasts keep working unchanged.
//
// Rounding matches Postgres round(numeric, 2) = half away from zero
// (NOT .NET banker's default).
public sealed record PricedLine(decimal UnitPrice, decimal FullPrice);

public static class OrderPricing
{
    public const decimal TaxRate = 0.08m;

    // Delivery is free (free-delivery migration zeroed the 1.50 fee;
    // mobile DELIVERY_FEE = 0). Kept as a named constant, not magic zero.
    public const decimal DeliveryFee = 0m;

    public static decimal Round2(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static PricedLine PriceLine(
        Coffee coffee,
        IReadOnlyList<CoffeeOption> storeOptions,
        Promotion? automaticPromo,
        string? size,
        string? temperature,
        string? milk,
        IEnumerable<string> extras)
    {
        var optionTotal = 0m;

        foreach (var (type, label) in new[] { ("size", size), ("temperature", temperature), ("milk", milk) })
        {
            var clean = label?.Trim();
            if (string.IsNullOrEmpty(clean))
                continue;
            optionTotal += FindDelta(storeOptions, type, clean, "Invalid coffee option");
        }

        foreach (var extra in extras)
        {
            var clean = extra.Trim();
            optionTotal += FindDelta(storeOptions, "extra", clean, "Invalid coffee extra");
        }

        var fullPrice = Round2(coffee.BasePrice + optionTotal);
        if (automaticPromo is null)
            return new PricedLine(fullPrice, fullPrice);

        var discountedBase = Round2(coffee.BasePrice * (1 - automaticPromo.DiscountPercent / 100));
        return new PricedLine(Round2(discountedBase + optionTotal), fullPrice);
    }

    private static decimal FindDelta(
        IReadOnlyList<CoffeeOption> storeOptions,
        string type,
        string label,
        string errorMessage)
    {
        var match = storeOptions
            .Where(o => o.Type == type
                && string.Equals(o.Label.Trim(), label, StringComparison.OrdinalIgnoreCase))
            .OrderBy(o => o.Id)
            .FirstOrDefault();
        if (match is null)
            throw new InvalidOperationException(errorMessage);
        return match.PriceDelta ?? 0m;
    }

    // Priority coffee > category > all, most recently created first.
    public static Promotion? BestAutomaticPromo(
        IEnumerable<Promotion> livePromos,
        Guid coffeeId,
        Guid? categoryId)
    {
        return livePromos
            .Where(p => p.Code is null
                && (p.Scope == "coffee" && p.CoffeeId == coffeeId
                    || p.Scope == "category" && categoryId.HasValue && p.CategoryId == categoryId
                    || p.Scope == "all"))
            .OrderBy(p => p.Scope == "coffee" ? 1 : p.Scope == "category" ? 2 : 3)
            .ThenByDescending(p => p.CreatedAt)
            .FirstOrDefault();
    }
}
