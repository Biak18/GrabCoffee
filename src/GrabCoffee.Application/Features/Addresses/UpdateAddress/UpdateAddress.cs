using FluentValidation;
using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Exceptions;
using MediatR;

namespace GrabCoffee.Application.Features.Addresses.UpdateAddress;

// Mirrors updateAddress (ownership enforced).
public sealed record UpdateAddressCommand(
    Guid Id,
    string Label,
    string FullName,
    string Phone,
    string StreetAddress,
    double? Lat,
    double? Lng) : IRequest<AddressDto>;

public sealed class UpdateAddressValidator : AbstractValidator<UpdateAddressCommand>
{
    public UpdateAddressValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.StreetAddress).NotEmpty().MaximumLength(500);
    }
}

public sealed class UpdateAddressHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<UpdateAddressCommand, AddressDto>
{
    public async Task<AddressDto> Handle(UpdateAddressCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        var address = await db.FindAddressAsync(request.Id, ct);
        if (address is null || address.UserId != userId)
            throw new NotFoundException("Address not found.");

        address.Label = request.Label.Trim();
        address.FullName = request.FullName.Trim();
        address.Phone = request.Phone.Trim();
        address.StreetAddress = request.StreetAddress.Trim();
        address.Lat = request.Lat;
        address.Lng = request.Lng;

        await db.SaveChangesAsync(ct);
        return AddressMapper.ToDto(address);
    }
}
