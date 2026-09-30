using FluentValidation;
using GrabCoffee.Application.Abstractions;
using MediatR;

namespace GrabCoffee.Application.Features.Orders.CancelOrder;

// Port of cancel_order(): buyer only, only while still received.
public sealed record CancelOrderCommand(Guid OrderId) : IRequest;

public sealed class CancelOrderValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}

public sealed class CancelOrderHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<CancelOrderCommand>
{
    public async Task Handle(CancelOrderCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        var order = await db.FindOrderAsync(request.OrderId, ct);
        if (order is null || order.UserId != userId || order.Status != "received")
            throw new InvalidOperationException("Order cannot be cancelled.");

        await db.ExecuteInOrderWriteTxAsync(
            _ =>
            {
                order.Status = "cancelled";
                return Task.CompletedTask;
            }, ct);
    }
}
