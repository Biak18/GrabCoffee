namespace GrabCoffee.Domain.Entities;

// Mirrors public.loyalty_events. Kind: earn | redeem.
public sealed class LoyaltyEvent
{
    public Guid Id { get; set; }
    public Guid CardId { get; set; }
    public Guid? OrderId { get; set; }
    public string Kind { get; set; } = "earn";
    public DateTime CreatedAt { get; set; }
}
