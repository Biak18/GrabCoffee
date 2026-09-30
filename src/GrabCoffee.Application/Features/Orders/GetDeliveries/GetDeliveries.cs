using GrabCoffee.Application.Abstractions;
using MediatR;

namespace GrabCoffee.Application.Features.Orders.GetDeliveries;

// Mirrors fetchDriverOrders: my active deliveries (assigned / out for delivery).
public sealed record GetDeliveriesQuery : IRequest<IReadOnlyList<OrderSummaryDto>>;

public sealed class GetDeliveriesHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetDeliveriesQuery, IReadOnlyList<OrderSummaryDto>>
{
    private static readonly string[] ActiveStatuses = ["driver_assigned", "out_for_delivery"];

    public async Task<IReadOnlyList<OrderSummaryDto>> Handle(GetDeliveriesQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        return await db.GetOrderSummariesAsync(null, null, userId, ActiveStatuses, page: 1, pageSize: 100, ct);
    }
}
