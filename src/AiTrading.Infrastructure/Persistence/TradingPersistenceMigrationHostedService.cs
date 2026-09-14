using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiTrading.Infrastructure.Persistence;

public sealed class TradingPersistenceMigrationHostedService(
    IDbContextFactory<TradingDbContext> contextFactory,
    ILogger<TradingPersistenceMigrationHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Trading persistence migrations applied successfully.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
