namespace AiTrading.Api;

public sealed record PaperShortCoverApiRequest(
    decimal CoverPrice,
    int CoverQuantity,
    long ExpectedVersion);
