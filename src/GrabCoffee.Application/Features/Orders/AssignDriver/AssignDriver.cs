using FluentValidation;
using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Exceptions;
using GrabCoffee.Application.Features.Stores;
using MediatR;

namespace GrabCoffee.Application.Features.Orders.AssignDriver;

// Port of assign_driver(): seller only, order must be a ready delivery,
// driver must exist. (The RPC does not require availability — kept as-is.)
public sealed record AssignDriverCommand(Guid OrderId, Guid DriverId) : IRequest;

public sealed class AssignDriverValidator : AbstractValidator<AssignDriverCommand>
{
    public AssignDriverValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.DriverId).NotEmpty();
    }
}

public sealed class AssignDriverHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<AssignDriverCommand>
{
    public async Task Handle(AssignDriverCommand request, CancellationToken ct)
    {
        var store = await StoreAccess.RequireMyStoreAsync(db, currentUser, ct);

        var order = await db.FindOrderAsync(request.OrderId, ct);
        if (order is null || order.StoreId != store.Id)
            throw new NotFoundException("Order not found.");
        if (order.Status != "ready")
            throw new InvalidOperationException("Order is not ready for assignment.");
        if (order.Fulfillment != "delivery")
            throw new InvalidOperationException("Order is not a delivery.");

        var driver = await db.FindDriverAsync(request.DriverId, ct);
        if (driver is null)
            throw new InvalidOperationException("Driver not found.");

        await db.ExecuteInOrderWriteTxAsync(
            _ =>
            {
                order.DriverId = request.DriverId;
                order.Status = "driver_assigned";
                return Task.CompletedTask;
            }, ct);
    }
}
