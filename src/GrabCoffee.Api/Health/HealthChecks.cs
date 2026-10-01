using System.Text.Json;
using GrabCoffee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GrabCoffee.Api.Health;

public sealed class PostgresHealthCheck(AppDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
        return HealthCheckResult.Healthy("Postgres reachable.");
    }
}

public static class HealthResponses
{
    public static string WriteJson(HealthReport report)
        => JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(
                e => e.Key,
                e => new
                {
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    durationMs = e.Value.Duration.TotalMilliseconds,
                }),
        });
}
