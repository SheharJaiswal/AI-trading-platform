using System.Net;
using System.Net.Http.Json;
using AiTrading.Application;
using AiTrading.Infrastructure;

namespace AiTrading.Application.Tests;

public sealed class ConfigurableAiProviderTests
{
    [Fact]
    public async Task ReturnsConfiguredProviderResult()
    {
        var expected = new AiResearchResult("local", "Use the deterministic evidence.", ["LOW_LIQUIDITY"], 0.72m, DateTimeOffset.UtcNow);
        using var client = new HttpClient(new StubHandler(HttpStatusCode.OK, expected));
        var provider = new ConfigurableAiProvider(client, new AiProviderOptions { Provider = "local", Endpoint = "http://localhost/research", ApiKey = "test-key" });

        var result = await provider.ResearchAsync(new AiResearchRequest("TCS", "Summarize", ["RSI=52"]), CancellationToken.None);

        Assert.Equal(expected.Provider, result.Provider);
        Assert.Equal(expected.Summary, result.Summary);
        Assert.Equal(expected.Risks, result.Risks);
        Assert.Equal(expected.Confidence, result.Confidence);
    }

    [Fact]
    public async Task ReturnsSafeResultWhenEndpointIsMissing()
    {
        var provider = new ConfigurableAiProvider(new HttpClient(new StubHandler(HttpStatusCode.OK, null)), new AiProviderOptions { Provider = "cloud" });

        var result = await provider.ResearchAsync(new AiResearchRequest("TCS", "Summarize", []), CancellationToken.None);

        Assert.Equal("cloud", result.Provider);
        Assert.Contains("AI_PROVIDER_ENDPOINT_NOT_CONFIGURED", result.Risks);
        Assert.Equal(0m, result.Confidence);
    }

    [Fact]
    public async Task ConvertsProviderFailureIntoNonAuthoritativeResult()
    {
        var provider = new ConfigurableAiProvider(new HttpClient(new StubHandler(HttpStatusCode.BadGateway, null)), new AiProviderOptions { Provider = "cloud", Endpoint = "http://localhost/research" });

        var result = await provider.ResearchAsync(new AiResearchRequest("TCS", "Summarize", []), CancellationToken.None);

        Assert.Equal("cloud", result.Provider);
        Assert.Contains("AI_PROVIDER_HTTP_502", result.Risks);
        Assert.Equal(0m, result.Confidence);
    }

    private sealed class StubHandler(HttpStatusCode statusCode, AiResearchResult? result) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode);
            if (result is not null) response.Content = JsonContent.Create(result);
            await Task.CompletedTask;
            return response;
        }
    }
}
