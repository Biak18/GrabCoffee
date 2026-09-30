namespace GrabCoffee.Domain.Entities;

// Mirrors public.stores. OwnerId == profiles.id of the seller.
public sealed class Store
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public string? HoursJson { get; set; }
    public Guid OwnerId { get; set; }
    public string? KpayPhone { get; set; }
    public string? PaymentNote { get; set; }
    public string? ContactPhone { get; set; }
}
