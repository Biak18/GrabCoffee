namespace GrabCoffee.Domain.Entities;

// Mirrors public.coffee_reviews (rating 1-5).
public sealed class CoffeeReview
{
    public Guid Id { get; set; }
    public Guid CoffeeId { get; set; }
    public Guid UserId { get; set; }
    public Guid OrderId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
