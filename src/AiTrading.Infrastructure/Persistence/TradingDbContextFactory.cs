using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AiTrading.Infrastructure.Persistence;

public sealed class TradingDbContextFactory : IDesignTimeDbContextFactory<TradingDbContext>
{
    public TradingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__MySql")
            ?? "Server=localhost;Port=3306;Database=ai_trading;User=root;Password=change-me;";

        // Design-time tooling must build the model without requiring a live database.
        // Keep runtime connection discovery separate from EF model construction.
        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 4, 0)))
            .Options;

        return new TradingDbContext(options);
    }
}
