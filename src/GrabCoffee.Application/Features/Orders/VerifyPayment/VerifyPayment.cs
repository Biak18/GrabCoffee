using FluentValidation;
using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Exceptions;
using GrabCoffee.Application.Features.Stores;
using MediatR;

namespace GrabCoffee.Application.Features.Orders.VerifyPayment;

// Port of set_payment_verified(): seller only, only while awaiting.
// Rejected payments fall back to unpaid so the buyer can resubmit.
public sealed record VerifyPaymentCommand(Guid OrderId, bool Verified) : IRequest;

public sealed class VerifyPaymentValidator : AbstractValidator<VerifyPaymentCommand>
{
    public VerifyPaymentValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}

public sealed class VerifyPaymentHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<VerifyPaymentCommand>
{
    public async Task Handle(VerifyPaymentCommand request, CancellationToken ct)
    {
        var store = await StoreAccess.RequireMyStoreAsync(db, currentUser, ct);

        var order = await db.FindOrderAsync(request.OrderId, ct);
        if (order is null || order.StoreId != store.Id)
            throw new NotFoundException("Order not found.");
        if (order.PaymentStatus != "awaiting_verification")
            throw new InvalidOperationException("Payment is not awaiting verification.");

        order.PaymentStatus = request.Verified ? "verified" : "unpaid";
        await db.SaveChangesAsync(ct);
    }
}
