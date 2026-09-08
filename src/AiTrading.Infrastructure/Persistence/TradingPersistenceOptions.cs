using Microsoft.Extensions.Configuration;

namespace AiTrading.Infrastructure.Persistence;

public sealed record TradingPersistenceOptions(bool Enabled)
{
    public static TradingPersistenceOptions FromConfiguration(IConfiguration configuration) =>
        new(bool.TryParse(configuration["Persistence:MySql:Enabled"], out var enabled) && enabled);
}
