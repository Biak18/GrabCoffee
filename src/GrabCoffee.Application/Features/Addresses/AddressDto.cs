using GrabCoffee.Domain.Entities;

namespace GrabCoffee.Application.Features.Addresses;

public sealed record AddressDto(
    Guid Id,
    string Label,
    string FullName,
    string Phone,
    string StreetAddress,
    double? Lat,
    double? Lng,
    bool IsDefault);

internal static class AddressMapper
{
    public static AddressDto ToDto(Address a) => new(
        a.Id, a.Label, a.FullName, a.Phone, a.StreetAddress,
        a.Lat, a.Lng, a.IsDefault);
}
