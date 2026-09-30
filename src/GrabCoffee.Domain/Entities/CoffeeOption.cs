namespace GrabCoffee.Domain.Entities;

// Mirrors public.coffee_options. Type: size | temperature | milk | extra.
public sealed class CoffeeOption
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public decimal? PriceDelta { get; set; }
    public Guid StoreId { get; set; }
}
