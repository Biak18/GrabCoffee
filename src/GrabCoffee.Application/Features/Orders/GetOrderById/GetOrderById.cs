using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Exceptions;
using MediatR;

namespace GrabCoffee.Application.Features.Orders.GetOrderById;

// Mirrors fetchOrderWithItems. Visible to buyer, store owner, assigned driver.
public sealed record GetOrderByIdQuery(Guid Id) : IRequest<OrderDetailsDto>;

public sealed class GetOrderByIdHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetOrderByIdQuery, OrderDetailsDto>
{
    public async Task<OrderDetailsDto> Handle(GetOrderByIdQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        var order = await db.GetOrderDetailsAsync(request.Id, ct);
        if (order is null)
            throw new NotFoundException("Order not found.");

        var store = await db.FindStoreAsync(order.StoreId, ct);
        var isOwner = store is not null && store.OwnerId == userId;
        var isDriver = order.DriverId.HasValue && order.DriverId.Value == userId;
        if (order.UserId != userId && !isOwner && !isDriver)
            throw new NotFoundException("Order not found.");

        return order;
    }
}
