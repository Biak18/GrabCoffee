using GrabCoffee.Application.Abstractions;
using MediatR;

namespace GrabCoffee.Application.Features.Stores.GetMyEarnings;

// Port of fetchSellerEarnings + computeSellerEarnings: narrow 30-day order
// rows aggregated server-side. Day boundaries use Myanmar time (+06:30),
// matching the device-local days sellers saw in the mobile computation.
public sealed record SellerEarningRow(string Status, decimal Total, DateTime? PlacedAt);

public sealed record SellerEarningsDto(
    decimal Today,
    decimal Week,
    decimal Month,
    decimal AvgOrder,
    int CompletedCount,
    int OpenOrders);

public sealed record GetMyEarningsQuery : IRequest<SellerEarningsDto>;

public sealed class GetMyEarningsHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetMyEarningsQuery, SellerEarningsDto>
{
    private static readonly TimeSpan YangonOffset = TimeSpan.FromHours(6.5);
    private static readonly HashSet<string> OpenStatuses =
        new(StringComparer.Ordinal) { "received", "preparing", "ready" };

    public async Task<SellerEarningsDto> Handle(GetMyEarningsQuery request, CancellationToken ct)
    {
        var store = await StoreAccess.RequireMyStoreAsync(db, currentUser, ct);

        var now = DateTimeOffset.UtcNow.ToOffset(YangonOffset);
        var todayStart = now.Date;
        var rows = await db.GetSellerEarningRowsAsync(
            store.Id, DateTime.SpecifyKind(todayStart.AddDays(-30), DateTimeKind.Utc), ct);

        var weekStart = todayStart.AddDays(-6);
        var monthStart = todayStart.AddDays(-29);

        var today = 0m;
        var week = 0m;
        var month = 0m;
        var monthCount = 0;
        var completedCount = 0;
        var openOrders = 0;

        foreach (var row in rows)
        {
            if (row.Status == "completed")
            {
                completedCount++;
                if (row.PlacedAt is not { } placedAt)
                    continue;
                var placed = new DateTimeOffset(placedAt, TimeSpan.Zero).ToOffset(YangonOffset);
                if (placed >= todayStart)
                {
                    today += row.Total;
                    week += row.Total;
                    month += row.Total;
                    monthCount++;
                }
                else if (placed >= weekStart)
                {
                    week += row.Total;
                    month += row.Total;
                    monthCount++;
                }
                else if (placed >= monthStart)
                {
                    month += row.Total;
                    monthCount++;
                }
            }
            else if (OpenStatuses.Contains(row.Status))
            {
                openOrders++;
            }
        }

        return new SellerEarningsDto(
            Round2(today),
            Round2(week),
            Round2(month),
            monthCount > 0 ? Round2(month / monthCount) : 0,
            completedCount,
            openOrders);
    }

    private static decimal Round2(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
