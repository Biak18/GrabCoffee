namespace GrabCoffee.Domain.Entities;

// Mirrors public.chat_messages (per-order chat).
public sealed class ChatMessage
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid SenderId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
