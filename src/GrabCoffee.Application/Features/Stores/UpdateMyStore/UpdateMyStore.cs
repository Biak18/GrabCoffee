using FluentValidation;
using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Exceptions;
using MediatR;

namespace GrabCoffee.Application.Features.Stores.UpdateMyStore;

public sealed record UpdateMyStoreCommand(
    string Name,
    string Address,
    StoreHoursDto? Hours,
    string? KpayPhone,
    string? PaymentNote,
    string? ContactPhone,
    double? Lat,
    double? Lng) : IRequest<StoreDto>;

public sealed class UpdateMyStoreValidator : AbstractValidator<UpdateMyStoreCommand>
{
    public UpdateMyStoreValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.KpayPhone).MaximumLength(50);
        RuleFor(x => x.ContactPhone).MaximumLength(50);
    }
}

public sealed class UpdateMyStoreHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<UpdateMyStoreCommand, StoreDto>
{
    public async Task<StoreDto> Handle(UpdateMyStoreCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        // Ownership is resolved server-side: callers can only ever touch
        // the store they own, the id is never trusted from the client.
        var store = await db.FindStoreByOwnerAsync(userId, ct)
            ?? throw new NotFoundException("No store found for this account.");

        store.Name = request.Name.Trim();
        store.Address = request.Address.Trim();
        store.HoursJson = StoreMapper.WriteHours(request.Hours);
        store.KpayPhone = string.IsNullOrWhiteSpace(request.KpayPhone) ? null : request.KpayPhone.Trim();
        store.PaymentNote = string.IsNullOrWhiteSpace(request.PaymentNote) ? null : request.PaymentNote.Trim();
        store.ContactPhone = string.IsNullOrWhiteSpace(request.ContactPhone) ? null : request.ContactPhone.Trim();
        store.Lat = request.Lat;
        store.Lng = request.Lng;

        await db.SaveChangesAsync(ct);
        return StoreMapper.ToDto(store);
    }
}
