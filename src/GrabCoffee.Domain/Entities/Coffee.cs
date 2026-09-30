namespace GrabCoffee.Domain.Entities;

// Mirrors public.coffees (the real menu items).
public sealed class Coffee
{
    public Guid Id { get; set; }
    public Guid? CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? Rating { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid StoreId { get; set; }
}
