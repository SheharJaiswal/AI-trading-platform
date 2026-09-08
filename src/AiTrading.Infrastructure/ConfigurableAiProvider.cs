using System.Net.Http.Json;
using AiTrading.Application;

namespace AiTrading.Infrastructure;

public sealed class AiProviderOptions
{
    public string Provider { get; init; } = "disabled";
    public string Endpoint { get; init; } = "";
    public string ApiKey { get; init; } = "";
    public int TimeoutSeconds { get; init; } = 30;
}

public sealed class ConfigurableAiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly AiProviderOptions _options;

    public ConfigurableAiProvider(HttpClient httpClient, AiProviderOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<AiResearchResult> ResearchAsync(AiResearchRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
            return new AiResearchResult(_options.Provider, "AI provider endpoint is not configured.", ["AI_PROVIDER_ENDPOINT_NOT_CONFIGURED"], 0m, DateTimeOffset.UtcNow);

        using var message = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
        {
            Content = JsonContent.Create(request)
        };
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            message.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_options.ApiKey}");

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new AiResearchResult(_options.Provider, "AI provider request failed.", [$"AI_PROVIDER_HTTP_{(int)response.StatusCode}"], 0m, DateTimeOffset.UtcNow);

        var result = await response.Content.ReadFromJsonAsync<AiResearchResult>(cancellationToken: cancellationToken);
        return result ?? new AiResearchResult(_options.Provider, "AI provider returned no research result.", ["AI_PROVIDER_EMPTY_RESPONSE"], 0m, DateTimeOffset.UtcNow);
    }
}
