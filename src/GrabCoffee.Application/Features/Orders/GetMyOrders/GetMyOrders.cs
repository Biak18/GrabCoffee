using FluentValidation;
using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Models;
using MediatR;

namespace GrabCoffee.Application.Features.Orders.GetMyOrders;

// Mirrors fetchMyPurchases(Page): own orders, newest first, paged.
public sealed record GetMyOrdersQuery(int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<OrderSummaryDto>>;

public sealed class GetMyOrdersValidator : AbstractValidator<GetMyOrdersQuery>
{
    public GetMyOrdersValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetMyOrdersHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetMyOrdersQuery, PagedResult<OrderSummaryDto>>
{
    public async Task<PagedResult<OrderSummaryDto>> Handle(GetMyOrdersQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        var orders = await db.GetOrderSummariesAsync(userId, null, null, null, request.Page, request.PageSize, ct);
        var total = await db.CountOrderSummariesAsync(userId, null, null, null, ct);
        return new PagedResult<OrderSummaryDto>(orders, request.Page, request.PageSize, total);
    }
}
