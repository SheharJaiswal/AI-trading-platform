namespace AiTrading.Infrastructure.Persistence;

public sealed record TradingPersistenceOptions(bool Enabled)
{
    public static TradingPersistenceOptions FromConfiguration(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        return new(configuration.GetValue<bool>("Persistence:MySql:Enabled"));
    }
}
