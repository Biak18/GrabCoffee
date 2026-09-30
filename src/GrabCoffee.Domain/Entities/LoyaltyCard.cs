namespace GrabCoffee.Domain.Entities;

// Mirrors public.loyalty_cards (stamps 0-10).
public sealed class LoyaltyCard
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid StoreId { get; set; }
    public int Stamps { get; set; }
    public DateTime UpdatedAt { get; set; }
}
