namespace GrabCoffee.Domain.Entities;

// Mirrors public.push_tokens.
public sealed class PushToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
