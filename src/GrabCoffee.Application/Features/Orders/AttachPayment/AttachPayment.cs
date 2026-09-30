using FluentValidation;
using GrabCoffee.Application.Abstractions;
using MediatR;

namespace GrabCoffee.Application.Features.Orders.AttachPayment;

// Port of attach_payment(): buyer only, while received, payment unset or
// still awaiting. Cash needs no proof (no call needed).
public sealed record AttachPaymentCommand(Guid OrderId, string Method, string Ref)
    : IRequest;

public sealed class AttachPaymentValidator : AbstractValidator<AttachPaymentCommand>
{
    public AttachPaymentValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Method)
            .Must(m => m == "kpay" || m == "mmqr")
            .WithMessage("Invalid payment details.");
        RuleFor(x => x.Ref).NotEmpty().WithMessage("Invalid payment details.");
    }
}

public sealed class AttachPaymentHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<AttachPaymentCommand>
{
    public async Task Handle(AttachPaymentCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        var order = await db.FindOrderAsync(request.OrderId, ct);
        if (order is null
            || order.UserId != userId
            || order.Status != "received"
            || (order.PaymentStatus is not ("unpaid" or "awaiting_verification")))
            throw new InvalidOperationException("Payment cannot be attached.");

        order.PaymentMethod = request.Method;
        order.PaymentRef = request.Ref.Trim();
        order.PaymentStatus = "awaiting_verification";
        await db.SaveChangesAsync(ct);
    }
}
