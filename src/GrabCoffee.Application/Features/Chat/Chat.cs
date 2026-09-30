using FluentValidation;
using GrabCoffee.Application.Abstractions;
using GrabCoffee.Application.Common.Exceptions;
using GrabCoffee.Domain.Entities;
using MediatR;

namespace GrabCoffee.Application.Features.Chat;

// Per-order chat (buyer + shop + assigned driver). Reads/writes go through
// the API; live updates stay on Supabase Realtime, unchanged.
public sealed record ChatMessageDto(
    Guid Id,
    Guid OrderId,
    Guid SenderId,
    string Body,
    DateTime CreatedAt,
    string SenderName);

public sealed record GetOrderMessagesQuery(Guid OrderId, int Limit = 50, DateTime? Before = null)
    : IRequest<IReadOnlyList<ChatMessageDto>>;

public sealed record SendOrderMessageCommand(Guid OrderId, string Body) : IRequest<ChatMessageDto>;

public sealed class GetOrderMessagesHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetOrderMessagesQuery, IReadOnlyList<ChatMessageDto>>
{
    public async Task<IReadOnlyList<ChatMessageDto>> Handle(GetOrderMessagesQuery request, CancellationToken ct)
    {
        await ChatAccess.RequireParticipantAsync(db, currentUser, request.OrderId, ct);

        var messages = await db.GetOrderMessagesAsync(
            request.OrderId, Math.Clamp(request.Limit, 1, 100), request.Before, ct);
        var names = await db.GetProfileNamesAsync(messages.Select(m => m.SenderId).Distinct(), ct);

        // Chronological for the client (stored newest-first for paging).
        return messages
            .Select(m => new ChatMessageDto(
                m.Id, m.OrderId, m.SenderId, m.Body, m.CreatedAt,
                names.GetValueOrDefault(m.SenderId) ?? "Coffee lover"))
            .Reverse()
            .ToList();
    }
}

public sealed class SendOrderMessageValidator : AbstractValidator<SendOrderMessageCommand>
{
    public SendOrderMessageValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(1000);
    }
}

public sealed class SendOrderMessageHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<SendOrderMessageCommand, ChatMessageDto>
{
    public async Task<ChatMessageDto> Handle(SendOrderMessageCommand request, CancellationToken ct)
    {
        var (order, userId) = await ChatAccess.RequireParticipantAsync(db, currentUser, request.OrderId, ct);

        // Mirrors normalizeMessageBody: collapse whitespace, cap length.
        var body = System.Text.RegularExpressions.Regex.Replace(request.Body, @"\s+", " ").Trim();
        if (body.Length > 1000)
            body = body[..1000];
        if (body.Length == 0)
            throw new InvalidOperationException("Message is empty.");

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            SenderId = userId,
            Body = body,
            CreatedAt = DateTime.UtcNow,
        };
        await db.AddChatMessageAsync(message, ct);
        await db.SaveChangesAsync(ct);

        var names = await db.GetProfileNamesAsync([userId], ct);
        return new ChatMessageDto(
            message.Id, message.OrderId, message.SenderId, message.Body, message.CreatedAt,
            names.GetValueOrDefault(userId) ?? "Coffee lover");
    }
}

file static class ChatAccess
{
    public static async Task<(Order Order, Guid UserId)> RequireParticipantAsync(
        IAppDbContext db, ICurrentUser currentUser, Guid orderId, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Not authenticated.");

        var order = await db.FindOrderAsync(orderId, ct);
        if (order is null)
            throw new NotFoundException("Order not found.");

        var store = await db.FindStoreAsync(order.StoreId, ct);
        var isOwner = store is not null && store.OwnerId == userId;
        var isDriver = order.DriverId.HasValue && order.DriverId.Value == userId;
        if (order.UserId != userId && !isOwner && !isDriver)
            throw new NotFoundException("Order not found.");

        return (order, userId);
    }
}
