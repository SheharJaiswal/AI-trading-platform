using AiTrading.Domain;

namespace AiTrading.Application;

public sealed class DurablePortfolioQueryService(ITradingUnitOfWorkFactory unitOfWorkFactory)
{
    public async Task<Portfolio?> GetAsync(Guid portfolioId, CancellationToken cancellationToken)
    {
        await using var unitOfWork = await unitOfWorkFactory.CreateAsync(cancellationToken);
        var portfolio = await unitOfWork.Portfolios.GetAsync(portfolioId, cancellationToken);
        if (portfolio is null) return null;

        var positions = await unitOfWork.Portfolios.GetOpenPositionsAsync(portfolioId, cancellationToken);
        var domainPositions = positions.Select(x => new Position(
            x.Id,
            x.Symbol,
            x.Quantity,
            x.AverageEntryPrice,
            x.StopLoss)).ToArray();
        var unrealized = positions.Sum(x => (x.CurrentMarketPrice - x.AverageEntryPrice) * x.Quantity);
        return new Portfolio(portfolio.Cash, domainPositions, unrealized, portfolio.RealizedPnl);
    }
}

public sealed class DurableAlertQueryService(ITradingUnitOfWorkFactory unitOfWorkFactory)
{
    public async Task<IReadOnlyList<Alert>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var unitOfWork = await unitOfWorkFactory.CreateAsync(cancellationToken);
        var alerts = await unitOfWork.Alerts.GetAllAsync(cancellationToken);
        return alerts.Select(x => new Alert(
            $"{x.PositionId}: {x.Rule}:{x.EvaluationBucket}",
            x.Severity,
            x.Message,
            x.CreatedAt,
            x.Symbol)).ToArray();
    }
}