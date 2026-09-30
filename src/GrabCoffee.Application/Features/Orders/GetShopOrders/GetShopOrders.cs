using FluentValidation;
using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Models;
using GrabCoffee.Application.Features.Stores;
using MediatR;

namespace GrabCoffee.Application.Features.Orders.GetShopOrders;

// Mirrors fetchMyShopOrders(Page): the seller's order queue, newest first.
public sealed record GetShopOrdersQuery(int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<OrderSummaryDto>>;

public sealed class GetShopOrdersValidator : AbstractValidator<GetShopOrdersQuery>
{
    public GetShopOrdersValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetShopOrdersHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetShopOrdersQuery, PagedResult<OrderSummaryDto>>
{
    public async Task<PagedResult<OrderSummaryDto>> Handle(GetShopOrdersQuery request, CancellationToken ct)
    {
        var store = await StoreAccess.RequireMyStoreAsync(db, currentUser, ct);

        var orders = await db.GetOrderSummariesAsync(null, store.Id, null, null, request.Page, request.PageSize, ct);
        var total = await db.CountOrderSummariesAsync(null, store.Id, null, null, ct);
        return new PagedResult<OrderSummaryDto>(orders, request.Page, request.PageSize, total);
    }
}
