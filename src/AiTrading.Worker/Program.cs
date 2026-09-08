using Microsoft.Extensions.Hosting;
using AiTrading.Application;
using AiTrading.Infrastructure;
using AiTrading.Infrastructure.Persistence;
using AiTrading.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHttpClient<IMarketDataProvider, AngelOneMarketDataProvider>();
builder.Services.AddSingleton(new AngelOneOptions
{
    ApiKey = builder.Configuration["AngelOne:ApiKey"] ?? "",
    AuthorizationToken = builder.Configuration["AngelOne:AuthorizationToken"] ?? "",
    BaseUrl = builder.Configuration["AngelOne:BaseUrl"] ?? "https://apiconnect.angelone.in"
});

var persistence = TradingPersistenceOptions.FromConfiguration(builder.Configuration);
if (persistence.Enabled)
{
    builder.Services.AddTradingMySqlPersistence(builder.Configuration);
    var portfolioId = Guid.Parse(builder.Configuration["Trading:PortfolioId"] ?? "00000000-0000-0000-0000-000000000001");
    builder.Services.AddSingleton<Func<CancellationToken, Task>>(sp =>
        new DurableRiskMonitor(
            sp.GetRequiredService<IMarketDataProvider>(),
            sp.GetRequiredService<ITradingUnitOfWorkFactory>(),
            portfolioId).CheckOnceAsync);
}
else
{
    builder.Services.AddSingleton<IPortfolio>(_ => new PaperPortfolio(1_000_000m));
    builder.Services.AddSingleton<IAlertStore, InMemoryAlertStore>();
    builder.Services.AddSingleton<RiskMonitor>();
    builder.Services.AddSingleton<Func<CancellationToken, Task>>(sp => sp.GetRequiredService<RiskMonitor>().CheckOnceAsync);
}

builder.Services.AddSingleton<IWorkerDelay, WorkerDelay>();
builder.Services.AddSingleton(MonitoringWorkerOptions.Default);
builder.Services.AddHostedService<MonitoringWorker>();
await builder.Build().RunAsync();
