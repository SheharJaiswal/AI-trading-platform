using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AiTrading.Infrastructure.Persistence;

public sealed class TradingDbContextFactory : IDesignTimeDbContextFactory<TradingDbContext>
{
    public TradingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__MySql")
            ?? "Server=localhost;Port=3306;Database=ai_trading;User=root;Password=change-me;";

        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
            .Options;

        return new TradingDbContext(options);
    }
}
