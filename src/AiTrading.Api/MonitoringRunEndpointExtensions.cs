using AiTrading.Application;

namespace AiTrading.Api;

public static class MonitoringRunEndpointExtensions
{
    public static void MapMonitoringRunEndpoints(this WebApplication app, bool persistenceEnabled)
    {
        app.MapGet("/api/monitoring/runs", async (string? limit, IServiceProvider services, CancellationToken cancellationToken) =>
        {
            if (!persistenceEnabled)
                return Results.Json(new { errorCode = "PERSISTENCE_DISABLED", message = "Monitoring-run history requires MySQL persistence." }, statusCode: 503);

            var requestedLimit = 20;
            if (limit is not null && (!int.TryParse(limit, out requestedLimit) || requestedLimit <= 0))
                return Results.BadRequest(new { errorCode = "INVALID_LIMIT", message = "Limit must be a positive integer." });

            try
            {
                var service = services.GetRequiredService<MonitoringRunService>();
                return Results.Ok(await service.GetRecentRunsAsync(requestedLimit, cancellationToken));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Results.BadRequest(new { errorCode = "INVALID_LIMIT", message = ex.Message });
            }
        });
    }
}
