namespace GrabCoffee.Domain.Entities;

// Mirrors public.coffee_option_categories (many-to-many).
public sealed class CoffeeOptionCategory
{
    public Guid OptionId { get; set; }
    public Guid CategoryId { get; set; }
}
