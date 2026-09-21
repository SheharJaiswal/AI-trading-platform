using AiTrading.Application;
using Moq;

namespace AiTrading.Application.Tests;

public sealed class DurablePortfolioInitializerTests
{
    [Fact]
    public async Task Initializes_Missing_Portfolio_With_Configured_Starting_Cash()
    {
        var portfolioId = Guid.NewGuid();
        var repository = new Mock<IPortfolioRepository>();
        var unitOfWork = new Mock<ITradingUnitOfWork>();
        var factory = new Mock<ITradingUnitOfWorkFactory>();

        repository.Setup(x => x.GetAsync(portfolioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PortfolioState?)null);
        unitOfWork.SetupGet(x => x.Portfolios).Returns(repository.Object);
        unitOfWork.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        factory.Setup(x => x.CreateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(unitOfWork.Object);

        await new DurablePortfolioInitializer(factory.Object, portfolioId, 1_000_000m)
            .InitializeAsync(CancellationToken.None);

        repository.Verify(x => x.SaveAsync(
            It.Is<PortfolioState>(p =>
                p.Id == portfolioId &&
                p.Cash == 1_000_000m &&
                p.RealizedPnl == 0m &&
                p.PeakEquity == 1_000_000m &&
                p.Version == 0),
            0,
            It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Does_Not_Overwrite_Existing_Portfolio()
    {
        var portfolioId = Guid.NewGuid();
        var repository = new Mock<IPortfolioRepository>();
        var unitOfWork = new Mock<ITradingUnitOfWork>();
        var factory = new Mock<ITradingUnitOfWorkFactory>();

        repository.Setup(x => x.GetAsync(portfolioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PortfolioState(portfolioId, 123_456m, 10m, DateTimeOffset.UtcNow, 4, 123_456m));
        unitOfWork.SetupGet(x => x.Portfolios).Returns(repository.Object);
        factory.Setup(x => x.CreateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(unitOfWork.Object);

        await new DurablePortfolioInitializer(factory.Object, portfolioId, 1_000_000m)
            .InitializeAsync(CancellationToken.None);

        repository.Verify(x => x.SaveAsync(
            It.IsAny<PortfolioState>(),
            It.IsAny<long>(),
            It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Concurrent_Initializer_Treats_Already_Created_Portfolio_As_Success()
    {
        var portfolioId = Guid.NewGuid();
        var repository = new Mock<IPortfolioRepository>();
        var unitOfWork = new Mock<ITradingUnitOfWork>();
        var factory = new Mock<ITradingUnitOfWorkFactory>();
        var existing = new PortfolioState(portfolioId, 1_000_000m, 0m, DateTimeOffset.UtcNow, 0, 1_000_000m);
        var reads = 0;

        repository.Setup(x => x.GetAsync(portfolioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Interlocked.Increment(ref reads) == 1 ? null : existing);
        unitOfWork.SetupGet(x => x.Portfolios).Returns(repository.Object);
        unitOfWork.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("duplicate key"));
        factory.Setup(x => x.CreateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(unitOfWork.Object);

        await new DurablePortfolioInitializer(factory.Object, portfolioId, 1_000_000m)
            .InitializeAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Defers_Initialization_When_MySql_Is_Unavailable()
    {
        var portfolioId = Guid.NewGuid();
        var repository = new Mock<IPortfolioRepository>();
        var unitOfWork = new Mock<ITradingUnitOfWork>();
        var factory = new Mock<ITradingUnitOfWorkFactory>();
        var databaseException = new InvalidOperationException(
            "An exception has been raised that is likely due to a transient failure.",
            new Exception("Unable to connect to any of the specified MySQL hosts."));

        repository.Setup(x => x.GetAsync(portfolioId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(databaseException);
        unitOfWork.SetupGet(x => x.Portfolios).Returns(repository.Object);
        factory.Setup(x => x.CreateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(unitOfWork.Object);

        await new DurablePortfolioInitializer(factory.Object, portfolioId, 1_000_000m)
            .InitializeAsync(CancellationToken.None);

        repository.Verify(x => x.SaveAsync(
            It.IsAny<PortfolioState>(),
            It.IsAny<long>(),
            It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
