using FluentValidation;
using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Exceptions;
using MediatR;

namespace GrabCoffee.Application.Features.Orders.UpdateOrderStatus;

// Port of update_order_status(): the exact transition matrix, seller or
// assigned driver only. Timestamps stamp on first entry (coalesce parity).
public sealed record UpdateOrderStatusCommand(Guid OrderId, string Status) : IRequest;

public sealed class UpdateOrderStatusValidator : AbstractValidator<UpdateOrderStatusCommand>
{
    private static readonly string[] AllowedStatuses =
    [
        "received", "preparing", "ready", "completed",
        "driver_assigned", "out_for_delivery", "delivered", "cancelled",
    ];

    public UpdateOrderStatusValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Status)
            .Must(s => AllowedStatuses.Contains(s.ToLowerInvariant()))
            .WithMessage("Invalid order status.");
    }
}

public sealed class UpdateOrderStatusHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<UpdateOrderStatusCommand>
{
    // (from, to) — verbatim from the RPC.
    private static readonly HashSet<(string From, string To)> AllowedTransitions =
    [
        ("received", "preparing"),
        ("preparing", "received"),
        ("preparing", "ready"),
        ("ready", "preparing"),
        ("ready", "completed"),
        ("ready", "driver_assigned"),
        ("driver_assigned", "ready"),
        ("driver_assigned", "out_for_delivery"),
        ("out_for_delivery", "driver_assigned"),
        ("out_for_delivery", "delivered"),
        ("out_for_delivery", "completed"),
        ("delivered", "out_for_delivery"),
        ("completed", "ready"),
    ];

    public async Task Handle(UpdateOrderStatusCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        var order = await db.FindOrderAsync(request.OrderId, ct);
        if (order is null)
            throw new NotFoundException("Order not found.");

        var store = await db.FindStoreAsync(order.StoreId, ct);
        var isOwner = store is not null && store.OwnerId == userId;
        var isDriver = order.DriverId.HasValue && order.DriverId.Value == userId;
        if (!isOwner && !isDriver)
            throw new NotFoundException("Order not found.");

        var target = request.Status.Trim().ToLowerInvariant();
        if (order.Status == "cancelled"
            || target == order.Status
            || !AllowedTransitions.Contains((order.Status, target)))
            throw new InvalidOperationException("Invalid order transition.");

        var now = DateTime.UtcNow;
        await db.ExecuteInOrderWriteTxAsync(
            _ =>
            {
                order.Status = target;
                if (target == "ready")
                    order.ReadyAt ??= now;
                else if (target == "completed")
                    order.CompletedAt ??= now;
                else if (target == "delivered")
                    order.DeliveredAt ??= now;
                return Task.CompletedTask;
            }, ct);
    }
}
