using FluentValidation;
using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Exceptions;
using MediatR;

namespace GrabCoffee.Application.Features.Reviews;

public sealed record CoffeeReviewDto(
    Guid Id,
    Guid CoffeeId,
    Guid UserId,
    int Rating,
    string? Comment,
    DateTime CreatedAt,
    string ReviewerName);

public sealed record GetCoffeeReviewsQuery(Guid CoffeeId, int Limit = 20)
    : IRequest<IReadOnlyList<CoffeeReviewDto>>;

public sealed record GetReviewedCoffeeIdsQuery(Guid OrderId)
    : IRequest<IReadOnlyList<Guid>>;

public sealed record SubmitReviewCommand(
    Guid CoffeeId,
    Guid OrderId,
    int Rating,
    string? Comment) : IRequest;

public sealed class GetCoffeeReviewsHandler(IAppDbContext db)
    : IRequestHandler<GetCoffeeReviewsQuery, IReadOnlyList<CoffeeReviewDto>>
{
    public async Task<IReadOnlyList<CoffeeReviewDto>> Handle(GetCoffeeReviewsQuery request, CancellationToken ct)
    {
        var reviews = await db.GetCoffeeReviewsAsync(request.CoffeeId, Math.Clamp(request.Limit, 1, 100), ct);
        var names = await db.GetProfileNamesAsync(reviews.Select(r => r.UserId).Distinct(), ct);
        return reviews.Select(r => new CoffeeReviewDto(
            r.Id, r.CoffeeId, r.UserId, r.Rating, r.Comment, r.CreatedAt,
            names.GetValueOrDefault(r.UserId) ?? "Coffee lover")).ToList();
    }
}

public sealed class GetReviewedCoffeeIdsHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetReviewedCoffeeIdsQuery, IReadOnlyList<Guid>>
{
    public async Task<IReadOnlyList<Guid>> Handle(GetReviewedCoffeeIdsQuery request, CancellationToken ct)
    {
        var order = await ReviewOrders.RequireMyOrderAsync(db, currentUser, request.OrderId, ct);
        return await db.GetReviewedCoffeeIdsAsync(order.Id, ct);
    }
}

public sealed class SubmitReviewValidator : AbstractValidator<SubmitReviewCommand>
{
    public SubmitReviewValidator()
    {
        RuleFor(x => x.CoffeeId).NotEmpty();
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comment).MaximumLength(2000);
    }
}

public sealed class SubmitReviewHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<SubmitReviewCommand>
{
    public async Task Handle(SubmitReviewCommand request, CancellationToken ct)
    {
        // Mirrors submit_coffee_review(): order must be mine, completed,
        // and actually contain this coffee.
        var order = await ReviewOrders.RequireMyOrderAsync(db, currentUser, request.OrderId, ct);
        if (order.Status != "completed")
            throw new InvalidOperationException("Only completed orders can be reviewed.");
        if (!await db.OrderContainsCoffeeAsync(order.Id, request.CoffeeId, ct))
            throw new InvalidOperationException("This order does not contain that coffee.");

        await db.AddReviewAsync(new GrabCoffee.Domain.Entities.CoffeeReview
        {
            Id = Guid.NewGuid(),
            CoffeeId = request.CoffeeId,
            UserId = order.UserId,
            OrderId = order.Id,
            Rating = request.Rating,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            CreatedAt = DateTime.UtcNow,
        }, ct);
        await db.SaveChangesAsync(ct);
    }
}

file static class ReviewOrders
{
    public static async Task<GrabCoffee.Domain.Entities.Order> RequireMyOrderAsync(
        IAppDbContext db, ICurrentUser currentUser, Guid orderId, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");
        var order = await db.FindOrderAsync(orderId, ct);
        if (order is null || order.UserId != userId)
            throw new NotFoundException("Order not found.");
        return order;
    }
}
