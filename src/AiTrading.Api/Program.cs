using System.Security.Cryptography;
using System.Text;
using AiTrading.Application;
using AiTrading.Domain;
using AiTrading.Infrastructure;
using AiTrading.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

var marketProvider = builder.Configuration["MarketData:Provider"]?.Trim().ToLowerInvariant() ?? "demo";
var maxAgeSeconds = builder.Configuration.GetValue<int?>("MarketData:MaxAgeSeconds") ?? 300;
if (maxAgeSeconds <= 0) throw new InvalidOperationException("MarketData:MaxAgeSeconds must be positive.");
builder.Services.AddSingleton(new MarketDataFreshnessOptions(TimeSpan.FromSeconds(maxAgeSeconds)));

if (marketProvider == "angelone")
{
    builder.Services.AddHttpClient<IMarketDataProvider, AngelOneMarketDataProvider>();
    builder.Services.AddSingleton(new AngelOneOptions
    {
        ApiKey = builder.Configuration["AngelOne:ApiKey"] ?? "",
        AuthorizationToken = builder.Configuration["AngelOne:AuthorizationToken"] ?? "",
        BaseUrl = builder.Configuration["AngelOne:BaseUrl"] ?? "https://apiconnect.angelone.in"
    });
}
else
{
    builder.Services.AddSingleton<IMarketDataProvider, DemoMarketDataProvider>();
}

var aiProvider = builder.Configuration["AI:Provider"]?.Trim().ToLowerInvariant() ?? "disabled";
if (aiProvider is not ("disabled" or "local" or "cloud"))
    throw new InvalidOperationException("AI:Provider must be one of: disabled, local, cloud.");
if (aiProvider == "disabled")
{
    builder.Services.AddSingleton<IAiProvider, DisabledAiProvider>();
}
else
{
    var aiOptions = new AiProviderOptions
    {
        Provider = aiProvider,
        Endpoint = builder.Configuration[$"AI:{aiProvider}:Endpoint"]?.Trim() ?? "",
        ApiKey = builder.Configuration[$"AI:{aiProvider}:ApiKey"] ?? "",
        TimeoutSeconds = builder.Configuration.GetValue<int?>($"AI:{aiProvider}:TimeoutSeconds") ?? 30
    };
    if (aiOptions.TimeoutSeconds <= 0) throw new InvalidOperationException($"AI:{aiProvider}:TimeoutSeconds must be positive.");
    builder.Services.AddSingleton(aiOptions);
    builder.Services.AddHttpClient<IAiProvider, ConfigurableAiProvider>(client => client.Timeout = TimeSpan.FromSeconds(aiOptions.TimeoutSeconds));
}

builder.Services.AddSingleton<RecommendationService>();
builder.Services.AddSingleton<RiskEngine>();
builder.Services.AddSingleton<IPaperExecutionProvider, InMemoryPaperExecutionProvider>();

var persistenceEnabled = TradingPersistenceOptions.FromConfiguration(builder.Configuration).Enabled;
if (persistenceEnabled)
{
    builder.Services.AddTradingMySqlPersistence(builder.Configuration);
    var startingCash = builder.Configuration.GetValue<decimal?>("Trading:StartingCash") ?? 1_000_000m;
    if (startingCash <= 0) throw new InvalidOperationException("Trading:StartingCash must be positive.");
    builder.Services.AddScoped<IPaperTradeService>(services => new DurablePaperTradingService(
        services.GetRequiredService<RecommendationService>(),
        services.GetRequiredService<RiskEngine>(),
        services.GetRequiredService<IPaperExecutionProvider>(),
        services.GetRequiredService<ITradingUnitOfWorkFactory>(),
        startingCash));
    builder.Services.AddScoped<DurablePortfolioQueryService>();
    builder.Services.AddScoped<DurableAlertQueryService>();
}
else
{
    builder.Services.AddSingleton<IPortfolio>(_ => new PaperPortfolio(1_000_000m));
    builder.Services.AddSingleton<IAlertStore, InMemoryAlertStore>();
    builder.Services.AddSingleton<RiskMonitor>();
    builder.Services.AddSingleton<PaperTradingService>();
}

var portfolioId = builder.Configuration.GetValue<Guid?>("Trading:PortfolioId") ?? Guid.Parse("00000000-0000-0000-0000-000000000001");

var app = builder.Build();
app.MapOpenApi();

app.MapGet("/health", () => Results.Ok(new { status = "ok", mode = "paper", marketProvider, aiProvider, persistence = persistenceEnabled }));
app.MapGet("/api/market/{symbol}/quote", async (string symbol, string? instrumentToken, IMarketDataProvider provider, CancellationToken ct) =>
    Results.Ok(await provider.GetQuoteAsync(new Symbol(symbol.ToUpperInvariant(), instrumentToken), ct)));
app.MapGet("/api/recommendations/{symbol}", async (string symbol, string? instrumentToken, RecommendationService service, CancellationToken ct) =>
    Results.Ok(await service.GetRecommendationAsync(new Symbol(symbol.ToUpperInvariant(), instrumentToken), ct)));
app.MapPost("/api/paper-trades/{symbol}", async (string symbol, string? instrumentToken, int quantity, HttpRequest request, IServiceProvider services, CancellationToken ct) =>
{
    if (quantity <= 0)
        return Results.BadRequest(new { errorCode = "INVALID_QUANTITY", message = "quantity must be positive" });
    if (!request.Headers.TryGetValue("Idempotency-Key", out var header) || string.IsNullOrWhiteSpace(header.ToString()) || header.ToString().Length > 128)
        return Results.BadRequest(new { errorCode = "INVALID_IDEMPOTENCY_KEY", message = "Idempotency-Key is required and must be 1-128 characters." });
    if (!persistenceEnabled)
        return Results.Json(new { errorCode = "PERSISTENCE_DISABLED", message = "Durable paper trading requires MySQL persistence to be enabled." }, statusCode: StatusCodes.Status503ServiceUnavailable);

    var idempotencyKey = header.ToString();
    var orderId = DeterministicGuid(idempotencyKey);
    try
    {
        var service = services.GetRequiredService<IPaperTradeService>();
        var result = await service.ExecuteAsync(portfolioId, orderId, idempotencyKey, new Symbol(symbol.ToUpperInvariant(), instrumentToken), quantity, ct);
        return result.Risk.Decision == RiskDecision.Approved
            ? Results.Ok(result)
            : Results.Json(new { errorCode = result.Risk.Decision.ToString().ToUpperInvariant(), message = result.Risk.Reason, risk = result.Risk }, statusCode: StatusCodes.Status422UnprocessableEntity);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("idempotency key", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Conflict(new { errorCode = "IDEMPOTENCY_CONFLICT", message = ex.Message });
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("without a fill", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Conflict(new { errorCode = "EXECUTION_RECONCILIATION_REQUIRED", message = ex.Message });
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("Insufficient virtual cash", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Json(new { errorCode = "INSUFFICIENT_CASH", message = ex.Message }, statusCode: StatusCodes.Status422UnprocessableEntity);
    }
});
app.MapPost("/api/ai/research", async (AiResearchRequest request, IAiProvider ai, CancellationToken ct) => Results.Ok(await ai.ResearchAsync(request, ct)));
app.MapGet("/api/portfolio", async (IServiceProvider services, CancellationToken ct) =>
{
    if (!persistenceEnabled)
        return Results.Ok(services.GetRequiredService<IPortfolio>().Snapshot());
    var portfolio = await services.GetRequiredService<DurablePortfolioQueryService>().GetAsync(portfolioId, ct);
    return portfolio is null ? Results.NotFound(new { errorCode = "PORTFOLIO_NOT_FOUND", message = $"Portfolio {portfolioId} does not exist." }) : Results.Ok(portfolio);
});
app.MapGet("/api/alerts", async (IServiceProvider services, CancellationToken ct) =>
{
    if (!persistenceEnabled)
        return Results.Ok(services.GetRequiredService<IAlertStore>().GetAll());
    return Results.Ok(await services.GetRequiredService<DurableAlertQueryService>().GetAllAsync(ct));
});

app.Run();

static Guid DeterministicGuid(string value)
{
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
    return new Guid(hash.AsSpan(0, 16));
}

public partial class Program { }
