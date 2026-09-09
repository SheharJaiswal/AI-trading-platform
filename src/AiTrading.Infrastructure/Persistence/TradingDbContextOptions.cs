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

        var serverVersion = ServerVersion.Parse(configuration["Persistence:MySql:ServerVersion"] ?? "8.0.0-mysql");
        services.AddDbContextFactory<TradingDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, mySql =>
                mySql.MigrationsAssembly(typeof(TradingDbContext).Assembly.GetName().Name)));
        services.AddSingleton<ITradingUnitOfWorkFactory, EfTradingUnitOfWorkFactory>();
        services.AddSingleton<IAlertDelivery, NoopAlertDelivery>();

        var portfolioId = configuration.GetValue<Guid?>("Trading:PortfolioId")
            ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
        services.AddScoped<DurableRiskMonitor>(sp => new DurableRiskMonitor(
            sp.GetRequiredService<IMarketDataProvider>(),
            sp.GetRequiredService<ITradingUnitOfWorkFactory>(),
            portfolioId,
            sp.GetRequiredService<IAlertDelivery>()));
        services.AddSingleton(TimeProvider.System);
        services.AddHostedService<DurableRiskMonitoringHostedService>();

        return services;
    }
}
