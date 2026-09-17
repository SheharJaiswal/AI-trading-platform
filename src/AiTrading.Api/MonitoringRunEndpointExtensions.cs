using AiTrading.Application;
using Microsoft.EntityFrameworkCore;

namespace AiTrading.Api;

public static class MonitoringRunEndpointExtensions
{
    public static void MapMonitoringRunEndpoints(this WebApplication app, bool persistenceEnabled)
    {
        app.MapGet("/api/monitoring/runs", async (string? limit, IServiceProvider services, CancellationToken cancellationToken) =>
        {
            var requestedLimit = 20;
            if (limit is not null && (!int.TryParse(limit, out requestedLimit) || requestedLimit <= 0))
                return Results.BadRequest(new { errorCode = "INVALID_LIMIT", message = "Limit must be a positive integer." });

            if (!persistenceEnabled)
                return Results.Json(new { errorCode = "PERSISTENCE_DISABLED", message = "Monitoring-run history requires MySQL persistence." }, statusCode: 503);

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

        app.MapGet("/api/monitoring/runs/{id:guid}", async (Guid id, IServiceProvider services, CancellationToken cancellationToken) =>
        {
            if (id == Guid.Empty)
                return Results.BadRequest(new { errorCode = "INVALID_MONITORING_RUN_ID", message = "Monitoring-run id must not be empty." });

            if (!persistenceEnabled)
                return Results.Json(new { errorCode = "PERSISTENCE_DISABLED", message = "Monitoring-run detail requires MySQL persistence." }, statusCode: 503);

            try
            {
                var run = await services.GetRequiredService<MonitoringRunService>().GetRunAsync(id, cancellationToken);
                return run is null
                    ? Results.NotFound(new { errorCode = "MONITORING_RUN_NOT_FOUND", message = $"Monitoring run {id} does not exist." })
                    : Results.Ok(run);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { errorCode = "INVALID_MONITORING_RUN_ID", message = ex.Message });
            }
        });

        app.MapPost("/api/paper-shorts/{positionId:guid}/cover", async (Guid positionId, PaperShortCoverApiRequest request, HttpRequest httpRequest, IServiceProvider services, CancellationToken cancellationToken) =>
        {
            if (!persistenceEnabled)
                return Results.Json(new { errorCode = "PERSISTENCE_DISABLED", message = "Paper short covering requires MySQL persistence." }, statusCode: 503);
            if (!httpRequest.Headers.TryGetValue("Idempotency-Key", out var header) || string.IsNullOrWhiteSpace(header.ToString()) || header.ToString().Length > 128)
                return Results.BadRequest(new { errorCode = "INVALID_IDEMPOTENCY_KEY", message = "Idempotency-Key is required and must be 1-128 characters." });
            try
            {
                var result = await services.GetRequiredService<DurablePaperShortCoverService>().CoverAsync(
                    positionId,
                    header.ToString(),
                    request.CoverPrice,
                    request.CoverQuantity,
                    request.ExpectedVersion,
                    DateTimeOffset.UtcNow,
                    cancellationToken);
                return Results.Ok(new { executionMode = "PAPER_ONLY", position = result });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { errorCode = "INVALID_SHORT_COVER", message = ex.Message });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Results.Conflict(new { errorCode = "SHORT_POSITION_CONCURRENCY_CONFLICT", message = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Idempotency key", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Conflict(new { errorCode = "IDEMPOTENCY_CONFLICT", message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { errorCode = "SHORT_COVER_REJECTED", message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { errorCode = "SHORT_POSITION_NOT_FOUND", message = ex.Message });
            }
        });
    }
}
