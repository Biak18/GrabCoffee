namespace GrabCoffee.Domain.Entities;

// Mirrors public.promotions. Scope: all | category | coffee.
public sealed class Promotion
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal DiscountPercent { get; set; }
    public string Scope { get; set; } = "all";
    public Guid? CategoryId { get; set; }
    public Guid? CoffeeId { get; set; }
    public DateOnly StartsAt { get; set; }
    public DateOnly EndsAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public Guid StoreId { get; set; }
    public string? Code { get; set; }
}
