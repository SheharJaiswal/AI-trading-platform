using AiTrading.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AiTrading.Infrastructure.Persistence;

public static class TradingPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddTradingMySqlPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MySql");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:MySql is required when MySQL persistence is enabled.");

        services.AddDbContextFactory<TradingDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
        services.AddScoped<ITradingUnitOfWorkFactory, EfTradingUnitOfWorkFactory>();

        return services;
    }
}
