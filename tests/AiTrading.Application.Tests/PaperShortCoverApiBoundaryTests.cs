using System.Net;
using AiTrading.Api;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AiTrading.Application.Tests;

public sealed class PaperShortCoverApiBoundaryTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Cover_Requires_Idempotency_Key_Before_Persistence_Access()
    {
        using var client = factory.CreateClient();
        using var content = new StringContent("{\"coverPrice\":98,\"coverQuantity\":1,\"expectedVersion\":0}", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"/api/paper-shorts/{Guid.NewGuid()}/cover", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("INVALID_IDEMPOTENCY_KEY", body, StringComparison.Ordinal);
    }
}
