namespace GrabCoffee.Domain.Entities;

// Mirrors public.drivers. Id == auth user id (role = driver).
public sealed class Driver
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Vehicle { get; set; }
    public bool IsAvailable { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
