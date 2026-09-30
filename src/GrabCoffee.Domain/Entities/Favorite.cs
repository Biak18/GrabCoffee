namespace GrabCoffee.Domain.Entities;

// Mirrors public.favorites (composite key user_id + coffee_id).
public sealed class Favorite
{
    public Guid UserId { get; set; }
    public Guid CoffeeId { get; set; }
    public DateTime? CreatedAt { get; set; }
}
