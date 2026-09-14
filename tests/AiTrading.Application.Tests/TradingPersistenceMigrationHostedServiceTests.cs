using AiTrading.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AiTrading.Application.Tests;

public sealed class TradingPersistenceMigrationHostedServiceTests
{
    [Fact]
    public void AddTradingMySqlPersistence_RegistersMigrationBeforeRiskMonitoring()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MySql"] = "Server=localhost;Port=3306;Database=trading;User=root;Password=test;",
                ["Persistence:MySql:ServerVersion"] = "8.0.0-mysql"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddTradingMySqlPersistence(configuration);

        var hostedServices = services
            .Where(d => d.ServiceType == typeof(IHostedService))
            .Select(d => d.ImplementationType)
            .ToArray();

        Assert.Equal(
            [
                typeof(TradingPersistenceMigrationHostedService),
                typeof(DurableRiskMonitoringHostedService)
            ],
            hostedServices);
    }
}
