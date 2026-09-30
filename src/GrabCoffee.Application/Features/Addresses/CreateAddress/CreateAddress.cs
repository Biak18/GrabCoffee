using FluentValidation;
using GrabCoffee.Application.Abstractions;
using GrabCoffee.Domain.Entities;
using MediatR;

namespace GrabCoffee.Application.Features.Addresses.CreateAddress;

// Mirrors createAddress (+ optional immediate default).
public sealed record CreateAddressCommand(
    string Label,
    string FullName,
    string Phone,
    string StreetAddress,
    double? Lat,
    double? Lng,
    bool IsDefault) : IRequest<AddressDto>;

public sealed class CreateAddressValidator : AbstractValidator<CreateAddressCommand>
{
    public CreateAddressValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.StreetAddress).NotEmpty().MaximumLength(500);
    }
}

public sealed class CreateAddressHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<CreateAddressCommand, AddressDto>
{
    public async Task<AddressDto> Handle(CreateAddressCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        var address = new Address
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Label = request.Label.Trim(),
            FullName = request.FullName.Trim(),
            Phone = request.Phone.Trim(),
            StreetAddress = request.StreetAddress.Trim(),
            Lat = request.Lat,
            Lng = request.Lng,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
        };
        await db.AddAddressAsync(address, ct);
        await db.SaveChangesAsync(ct);

        if (request.IsDefault)
            await db.SetDefaultAddressAsync(userId, address.Id, ct);

        var created = await db.FindAddressAsync(address.Id, ct);
        return AddressMapper.ToDto(created ?? address);
    }
}
