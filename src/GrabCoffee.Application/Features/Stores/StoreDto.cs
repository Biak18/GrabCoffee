using System.Text.Json;
using GrabCoffee.Domain.Entities;

namespace GrabCoffee.Application.Features.Stores;

public sealed record StoreHoursDto(string Open, string Close);

public sealed record StoreDto(
    Guid Id,
    string Name,
    string Address,
    StoreHoursDto? Hours,
    string? KpayPhone,
    string? PaymentNote,
    string? ContactPhone,
    double? Lat,
    double? Lng);

internal static class StoreMapper
{
    // Mobile reads hours.open / hours.close (camelCase) — match it exactly.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static StoreDto ToDto(Store store) => new(
        store.Id,
        store.Name,
        store.Address,
        ParseHours(store.HoursJson),
        store.KpayPhone,
        store.PaymentNote,
        store.ContactPhone,
        store.Lat,
        store.Lng);

    public static StoreHoursDto? ParseHours(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<StoreHoursDto>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string? WriteHours(StoreHoursDto? hours)
        => hours is null ? null : JsonSerializer.Serialize(hours, JsonOptions);
}
