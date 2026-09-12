using AiTrading.Application;

namespace AiTrading.Application.Tests;

public sealed class PaperShortMarketDataGuardTests
{
    [Fact]
    public void Validate_FreshDataIsApproved()
    {
        var now = new DateTimeOffset(2026, 9, 12, 0, 0, 0, TimeSpan.Zero);
        var result = PaperShortMarketDataGuard.Validate(now.AddSeconds(-30), now, TimeSpan.FromMinutes(1));

        Assert.True(result.Approved);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void Validate_StaleDataIsRejected()
    {
        var now = new DateTimeOffset(2026, 9, 12, 0, 0, 0, TimeSpan.Zero);
        var result = PaperShortMarketDataGuard.Validate(now.AddMinutes(-2), now, TimeSpan.FromMinutes(1));

        Assert.False(result.Approved);
        Assert.Equal("STALE_MARKET_DATA", result.Reason);
    }

    [Fact]
    public void Validate_FutureDataIsRejected()
    {
        var now = new DateTimeOffset(2026, 9, 12, 0, 0, 0, TimeSpan.Zero);
        var result = PaperShortMarketDataGuard.Validate(now.AddSeconds(1), now, TimeSpan.FromMinutes(1));

        Assert.False(result.Approved);
        Assert.Equal("FUTURE_MARKET_DATA", result.Reason);
    }

    [Fact]
    public void Validate_NegativeMaxAgeIsRejected()
    {
        var now = new DateTimeOffset(2026, 9, 12, 0, 0, 0, TimeSpan.Zero);
        var result = PaperShortMarketDataGuard.Validate(now, now, TimeSpan.FromSeconds(-1));

        Assert.False(result.Approved);
        Assert.Equal("INVALID_MAX_AGE", result.Reason);
    }
}
