namespace AiTrading.Application;

public sealed class DurablePortfolioInitializer(
    ITradingUnitOfWorkFactory unitOfWorkFactory,
    Guid portfolioId,
    decimal startingCash)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (portfolioId == Guid.Empty)
            throw new ArgumentException("Portfolio id must be non-empty.", nameof(portfolioId));
        if (startingCash <= 0)
            throw new ArgumentOutOfRangeException(nameof(startingCash), "Starting cash must be positive.");

        await using var unitOfWork = await unitOfWorkFactory.CreateAsync(cancellationToken);
        var existing = await unitOfWork.Portfolios.GetAsync(portfolioId, cancellationToken);
        if (existing is not null)
            return;

        var now = DateTimeOffset.UtcNow;
        await unitOfWork.Portfolios.SaveAsync(
            new PortfolioState(portfolioId, startingCash, 0m, now, 0, startingCash),
            0,
            cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
    }
}
