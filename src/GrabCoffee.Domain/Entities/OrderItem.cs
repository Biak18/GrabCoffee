namespace GrabCoffee.Domain.Entities;

// Mirrors public.order_items.
public sealed class OrderItem
{
    public Guid Id { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? CoffeeId { get; set; }
    public string? Size { get; set; }
    public string? Temperature { get; set; }
    public string? Milk { get; set; }
    public string[]? Extras { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal? CompareAtPrice { get; set; }
}
