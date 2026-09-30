namespace GrabCoffee.Domain.Entities;

// Mirrors public.store_favorites (composite key user_id + store_id).
public sealed class StoreFavorite
{
    public Guid UserId { get; set; }
    public Guid StoreId { get; set; }
    public DateTime CreatedAt { get; set; }
}
