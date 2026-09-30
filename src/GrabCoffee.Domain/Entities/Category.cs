namespace GrabCoffee.Domain.Entities;

// Mirrors public.categories (menu categories per store).
public sealed class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public Guid StoreId { get; set; }
}
