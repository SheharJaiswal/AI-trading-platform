using AiTrading.Application;

namespace AiTrading.Application.Tests;

public class AiGatewayTests
{
    [Fact]
    public async Task DisabledProvider_IsExplicit_And_Never_Approves_Trading()
    {
        var provider = new DisabledAiProvider();
        var result = await provider.ResearchAsync(new AiResearchRequest("DEMO", "Assess trend", ["SMA20"]), CancellationToken.None);

        Assert.Equal("disabled", result.Provider);
        Assert.Equal(0m, result.Confidence);
        Assert.Contains("AI_PROVIDER_NOT_CONFIGURED", result.Risks);
    }
}
