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
        PortfolioState? existing;
        try
        {
            existing = await unitOfWork.Portfolios.GetAsync(portfolioId, cancellationToken);
        }
        catch (InvalidOperationException ex) when (!cancellationToken.IsCancellationRequested &&
            ex.InnerException?.Message.Contains("Unable to connect to any of the specified MySQL hosts", StringComparison.OrdinalIgnoreCase) == true)
        {
            // Persistence startup is allowed to defer initialization while MySQL is unavailable.
            // Readiness remains responsible for reporting the dependency as unavailable.
            return;
        }

        if (existing is not null)
            return;

        var now = DateTimeOffset.UtcNow;
        await unitOfWork.Portfolios.SaveAsync(
            new PortfolioState(portfolioId, startingCash, 0m, now, 0, startingCash),
            0,
            cancellationToken);

        try
        {
            await unitOfWork.CommitAsync(cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            await using var verificationUnitOfWork = await unitOfWorkFactory.CreateAsync(cancellationToken);
            if (await verificationUnitOfWork.Portfolios.GetAsync(portfolioId, cancellationToken) is not null)
                return;

            throw;
        }
    }
}
