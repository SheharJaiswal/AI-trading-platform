using AiTrading.Application;
using AiTrading.Domain;
using AiTrading.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddHttpClient<IMarketDataProvider, AngelOneMarketDataProvider>();
builder.Services.AddSingleton(new AngelOneOptions
{
    ApiKey = builder.Configuration["AngelOne:ApiKey"] ?? "",
    AuthorizationToken = builder.Configuration["AngelOne:AuthorizationToken"] ?? "",
    BaseUrl = builder.Configuration["AngelOne:BaseUrl"] ?? "https://apiconnect.angelone.in"
});
builder.Services.AddSingleton<RecommendationService>();
builder.Services.AddSingleton<RiskEngine>();
builder.Services.AddSingleton<IPaperExecutionProvider, InMemoryPaperExecutionProvider>();
builder.Services.AddSingleton<IPortfolio>(_ => new PaperPortfolio(1_000_000m));
builder.Services.AddSingleton<IAlertStore, InMemoryAlertStore>();
builder.Services.AddSingleton<RiskMonitor>();
builder.Services.AddSingleton<PaperTradingService>();
builder.Services.AddHostedService<RiskMonitoringHostedService>();

var app = builder.Build();
app.MapOpenApi();

app.MapGet("/api/market/{symbol}/quote", async (string symbol, string instrumentToken, IMarketDataProvider provider, CancellationToken ct) =>
    Results.Ok(await provider.GetQuoteAsync(new Symbol(symbol.ToUpperInvariant(), instrumentToken), ct)));

app.MapGet("/api/recommendations/{symbol}", async (string symbol, string instrumentToken, RecommendationService service, CancellationToken ct) =>
    Results.Ok(await service.GetRecommendationAsync(new Symbol(symbol.ToUpperInvariant(), instrumentToken), ct)));

app.MapPost("/api/paper-trades/{symbol}", async (string symbol, string instrumentToken, int quantity, PaperTradingService service, CancellationToken ct) =>
{
    var result = await service.ExecuteAsync(new Symbol(symbol.ToUpperInvariant(), instrumentToken), quantity, ct);
    return Results.Ok(result);
});

app.MapGet("/api/portfolio", (IPortfolio portfolio) => Results.Ok(portfolio.Snapshot()));
app.MapGet("/api/alerts", (IAlertStore alerts) => Results.Ok(alerts.GetAll()));

app.Run();

public partial class Program { }
