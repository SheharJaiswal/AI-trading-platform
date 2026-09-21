using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AiTrading.Infrastructure.Persistence;

public static class DatabaseMigrationExtensions
{
    public static async Task MigrateAsync(this DatabaseFacade database, CancellationToken cancellationToken)
    {
        try
        {
            if (!await database.CanConnectAsync(cancellationToken))
                return;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return;
        }

        await database.GetService<IMigrator>().MigrateAsync(null, cancellationToken);
    }
}
