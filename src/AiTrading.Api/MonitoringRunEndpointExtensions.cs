using AiTrading.Application;
using Microsoft.AspNetCore.Http;

namespace AiTrading.Api;

public static class MonitoringRunEndpointExtensions
{
    public static void MapMonitoringRunEndpoints(this WebApplication app)
    {
        app.MapGet("/api/monitoring/runs", async (int? limit, MonitoringRunService service, CancellationToken cancellationToken) =>
        {
            var requestedLimit = limit ?? 20;
            try
            {
                return Results.Ok(await service.GetRecentRunsAsync(requestedLimit, cancellationToken));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Results.BadRequest(new { errorCode = "INVALID_LIMIT", message = ex.Message });
            }
        });
    }
}
