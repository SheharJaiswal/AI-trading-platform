using AiTrading.Application;
using AiTrading.Infrastructure;
using AiTrading.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHttpClient<IMarketDataProvider, AngelOneMarketDataProvider>();
builder.Services.AddSingleton(new AngelOneOptions
{
    ApiKey = builder.Configuration["AngelOne:ApiKey"] ?? "",
    AuthorizationToken = builder.Configuration["AngelOne:AuthorizationToken"] ?? "",
    BaseUrl = builder.Configuration["AngelOne:BaseUrl"] ?? "https://apiconnect.angelone.in"
});
builder.Services.AddSingleton<IPortfolio>(_ => new PaperPortfolio(1_000_000m));
builder.Services.AddSingleton<IAlertStore, InMemoryAlertStore>();
builder.Services.AddSingleton<RiskMonitor>();
builder.Services.AddHostedService<MonitoringWorker>();
await builder.Build().RunAsync();
