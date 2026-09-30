using FluentValidation;
using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Features.Orders.Pricing;
using GrabCoffee.Domain.Entities;
using MediatR;

namespace GrabCoffee.Application.Features.Orders.PlaceOrder;

// Port of create_order(): the server reprices everything — client totals are
// never trusted (and not accepted). Per-line client UnitPrice is cross-checked
// for cart freshness ("Menu price changed") — refresh via price-check first.
public sealed record PlaceOrderLineInput(
    Guid CoffeeId,
    string? Size,
    string? Temperature,
    string? Milk,
    string[]? Extras,
    int Quantity,
    decimal UnitPrice);

public sealed record PlaceOrderCommand(
    Guid StoreId,
    string Fulfillment,
    List<PlaceOrderLineInput> Items,
    decimal Tip,
    string? PromoCode,
    bool RedeemLoyalty,
    string? IdempotencyKey,
    string? DeliveryAddress,
    double? DeliveryLat,
    double? DeliveryLng) : IRequest<Guid>;

public sealed class PlaceOrderValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.Fulfillment)
            .Must(f => f == "pickup" || f == "delivery")
            .WithMessage("Invalid fulfillment.");
        RuleFor(x => x.Items).NotEmpty().WithMessage("Order must contain items.");
        RuleForEach(x => x.Items).ChildRules(line =>
        {
            line.RuleFor(l => l.CoffeeId).NotEmpty();
            line.RuleFor(l => l.Quantity).InclusiveBetween(1, 99).WithMessage("Invalid quantity.");
        });
        RuleFor(x => x.Tip).InclusiveBetween(0, 5000).WithMessage("Invalid tip.");
        RuleFor(x => x.IdempotencyKey).MaximumLength(100).WithMessage("Invalid idempotency key.");
        RuleFor(x => x.DeliveryAddress)
            .NotEmpty().When(x => x.Fulfillment == "delivery")
            .WithMessage("Delivery address required.");
    }
}

public sealed class PlaceOrderHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<PlaceOrderCommand, Guid>
{
    public async Task<Guid> Handle(PlaceOrderCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        var fulfillment = request.Fulfillment.Trim().ToLowerInvariant();
        var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? null : request.IdempotencyKey.Trim();
        var promoCode = string.IsNullOrWhiteSpace(request.PromoCode)
            ? null : request.PromoCode.Trim();
        var tip = request.Tip;

        if (idempotencyKey is not null)
        {
            var existing = await db.FindOrderByIdempotencyAsync(userId, idempotencyKey, ct);
            if (existing is not null)
                return existing.Id;
        }

        var store = await db.FindStoreAsync(request.StoreId, ct)
            ?? throw new InvalidOperationException("Store not found.");

        var deliveryAddress = string.IsNullOrWhiteSpace(request.DeliveryAddress)
            ? null : request.DeliveryAddress.Trim();
        if (fulfillment == "delivery" && deliveryAddress is null)
            throw new InvalidOperationException("Delivery address required.");
        if (fulfillment == "pickup" && request.DeliveryAddress is not null)
            throw new InvalidOperationException("Pickup orders cannot include a delivery address.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var coffees = (await db.FindActiveCoffeesAsync(
                store.Id, request.Items.Select(i => i.CoffeeId).Distinct(), ct))
            .ToDictionary(c => c.Id);
        var options = await db.GetStoreOptionsAsync(store.Id, ct);
        var autoPromos = await db.GetActivePromotionsAsync(store.Id, today, ct);

        var subtotal = 0m;
        decimal? cheapestUnit = null;
        var pricedLines = new List<(PlaceOrderLineInput Line, Coffee Coffee, decimal UnitPrice)>();
        foreach (var line in request.Items)
        {
            if (!coffees.TryGetValue(line.CoffeeId, out var coffee))
                throw new InvalidOperationException("Coffee is unavailable for this store.");

            var promo = OrderPricing.BestAutomaticPromo(autoPromos, coffee.Id, coffee.CategoryId);
            var priced = OrderPricing.PriceLine(
                coffee, options, promo,
                line.Size, line.Temperature, line.Milk, line.Extras ?? []);

            if (OrderPricing.Round2(line.UnitPrice) != priced.UnitPrice)
                throw new InvalidOperationException("Menu price changed; refresh your cart.");

            subtotal += priced.UnitPrice * line.Quantity;
            cheapestUnit = cheapestUnit is null ? priced.UnitPrice : Math.Min(cheapestUnit.Value, priced.UnitPrice);
            pricedLines.Add((line, coffee, priced.UnitPrice));
        }
        subtotal = OrderPricing.Round2(subtotal);
        var tax = OrderPricing.Round2(subtotal * OrderPricing.TaxRate);

        var promoDiscount = 0m;
        if (promoCode is not null)
        {
            var voucher = await db.LookupPromoCodeAsync(store.Id, promoCode, today, ct)
                ?? throw new InvalidOperationException("Promotion is not valid.");
            if (voucher.Scope is not ("all" or "category" or "coffee"))
                throw new InvalidOperationException("Promotion scope is invalid.");

            var eligible = 0m;
            foreach (var (line, coffee, unitPrice) in pricedLines)
            {
                if (voucher.Scope == "all"
                    || (voucher.Scope == "coffee" && voucher.CoffeeId == line.CoffeeId)
                    || (voucher.Scope == "category" && voucher.CategoryId == coffee.CategoryId))
                    eligible += unitPrice * line.Quantity;
            }
            promoDiscount = OrderPricing.Round2(eligible * voucher.DiscountPercent / 100);
        }

        var loyaltyDiscount = 0m;
        if (request.RedeemLoyalty)
        {
            var card = await db.FindLoyaltyCardAsync(userId, store.Id, ct);
            if (card is null || card.Stamps < 10)
                throw new InvalidOperationException("Loyalty reward is no longer available.");
            loyaltyDiscount = Math.Min(cheapestUnit ?? 0m, subtotal - promoDiscount);
        }

        var discount = OrderPricing.Round2(promoDiscount + loyaltyDiscount);
        var total = OrderPricing.Round2(
            subtotal + tax + OrderPricing.DeliveryFee + tip - discount);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StoreId = store.Id,
            Status = "received",
            Fulfillment = fulfillment,
            Subtotal = subtotal,
            Tax = tax,
            Total = total,
            Discount = discount,
            Tip = tip,
            PromoCode = promoCode,
            DeliveryFee = OrderPricing.DeliveryFee,
            DeliveryAddress = fulfillment == "delivery" ? deliveryAddress : null,
            DeliveryLat = fulfillment == "delivery" ? request.DeliveryLat : null,
            DeliveryLng = fulfillment == "delivery" ? request.DeliveryLng : null,
            IdempotencyKey = idempotencyKey,
            PlacedAt = DateTime.UtcNow,
            PaymentMethod = "cash",
            PaymentStatus = "unpaid",
        };

        var items = pricedLines.Select(p => new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            CoffeeId = p.Line.CoffeeId,
            Size = Clean(p.Line.Size),
            Temperature = Clean(p.Line.Temperature),
            Milk = Clean(p.Line.Milk),
            Extras = (p.Line.Extras ?? []).Select(e => e.Trim()).ToArray(),
            Quantity = p.Line.Quantity,
            UnitPrice = p.UnitPrice,
            CreatedAt = DateTime.UtcNow,
        }).ToList();

        return await db.InsertOrderAsync(order, items, request.RedeemLoyalty, ct);
    }

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
