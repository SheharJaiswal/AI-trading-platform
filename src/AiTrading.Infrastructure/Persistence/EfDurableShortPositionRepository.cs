using System.Data;
using AiTrading.Application;
using AiTrading.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data.Common;

namespace AiTrading.Infrastructure.Persistence;

public sealed class EfDurableShortPositionRepository(TradingDbContext db) : IDurableShortPositionRepository
{
    public async Task<DurableShortPositionState?> GetAsync(Guid positionId, CancellationToken ct) => await ReadPositionAsync(positionId, forUpdate: false, ct);

    public async Task<IReadOnlyList<DurableShortCoverState>> GetCoversAsync(Guid positionId, CancellationToken ct)
    {
        if (positionId == Guid.Empty) throw new ArgumentException("Position id must not be empty.", nameof(positionId));
        await using var command = CreateCommand("SELECT Id, PositionId, IdempotencyKey, CoverPrice, CoverQuantity, RealizedPnl, ResultingPositionVersion, CreatedAt FROM paper_short_covers WHERE PositionId=@position ORDER BY CreatedAt, Id");
        Add(command, "@position", positionId.ToString());
        await EnsureOpenAsync(command.Connection!, ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var covers = new List<DurableShortCoverState>();
        while (await reader.ReadAsync(ct))
        {
            var cover = new DurableShortCoverState(ParseGuid(reader.GetValue(0)), ParseGuid(reader.GetValue(1)), reader.GetString(2), reader.GetDecimal(3), reader.GetInt32(4), reader.GetDecimal(5), reader.GetInt64(6), ReadDate(reader, 7));
            ValidateCoverState(cover, positionId);
            covers.Add(cover);
        }
        return covers;
    }

    public async Task AddAsync(DurableShortPositionState position, CancellationToken ct)
    {
        ValidatePositionState(position);
        await using var command = CreateCommand("INSERT INTO paper_short_positions (Id, PortfolioId, Symbol, InstrumentToken, OriginalQuantity, RemainingQuantity, AverageEntryPrice, StopLoss, TargetPrice, LastCoverPrice, RealizedPnl, State, CreatedAt, UpdatedAt, Version) VALUES (@id,@portfolio,@symbol,@token,@original,@remaining,@entry,@stopLoss,@targetPrice,@lastCover,@pnl,@state,@created,@updated,@version)");
        Add(command, "@id", position.Id.ToString()); Add(command, "@portfolio", position.PortfolioId.ToString()); Add(command, "@symbol", position.Symbol.Value); Add(command, "@token", (object?)position.Symbol.InstrumentToken ?? DBNull.Value); Add(command, "@original", position.OriginalQuantity); Add(command, "@remaining", position.RemainingQuantity); Add(command, "@entry", position.AverageEntryPrice); Add(command, "@stopLoss", position.StopLoss is null ? DBNull.Value : position.StopLoss.Value); Add(command, "@targetPrice", position.TargetPrice is null ? DBNull.Value : position.TargetPrice.Value); Add(command, "@lastCover", position.LastCoverPrice is null ? DBNull.Value : position.LastCoverPrice.Value); Add(command, "@pnl", position.RealizedPnl); Add(command, "@state", position.State); Add(command, "@created", position.CreatedAt.UtcDateTime); Add(command, "@updated", position.UpdatedAt.UtcDateTime); Add(command, "@version", position.Version);
        await EnsureOpenAsync(command.Connection!, ct); await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<DurableShortPositionState> ApplyCoverAsync(Guid positionId, string idempotencyKey, decimal coverPrice, int coverQuantity, long expectedVersion, DateTimeOffset now, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 128) throw new ArgumentException("Idempotency key is required and must be 1-128 characters.", nameof(idempotencyKey));
        if (coverPrice <= 0) throw new ArgumentOutOfRangeException(nameof(coverPrice), "Cover price must be positive.");
        if (coverQuantity <= 0) throw new ArgumentOutOfRangeException(nameof(coverQuantity), "Cover quantity must be positive.");

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var existing = await GetCoverAsync(positionId, idempotencyKey, ct);
        if (existing is not null)
        {
            if (existing.CoverPrice != coverPrice || existing.CoverQuantity != coverQuantity) throw new InvalidOperationException("Idempotency key is already associated with a different cover operation.");
            var replay = await ReadPositionAsync(positionId, forUpdate: false, ct) ?? throw new InvalidOperationException("Persisted cover exists without its short position.");
            await transaction.CommitAsync(ct); return replay;
        }

        var position = await ReadPositionAsync(positionId, forUpdate: true, ct) ?? throw new KeyNotFoundException($"Paper short position {positionId} does not exist.");
        if (position.Version != expectedVersion) throw new DbUpdateConcurrencyException("Paper short position version is stale.");
        if (position.RemainingQuantity <= 0) throw new InvalidOperationException("The paper short position is already closed.");
        if (coverQuantity > position.RemainingQuantity) throw new InvalidOperationException("Cover quantity cannot exceed the remaining short quantity.");

        var coveredPnl = PaperShortAccounting.RealizedPnl(position.AverageEntryPrice, coverPrice, coverQuantity);
        var remaining = position.RemainingQuantity - coverQuantity;
        var updated = position with { RemainingQuantity = remaining, LastCoverPrice = coverPrice, RealizedPnl = position.RealizedPnl + coveredPnl, State = remaining == 0 ? "SHORT_CLOSED" : "SHORT_PARTIALLY_COVERED", UpdatedAt = now, Version = checked(position.Version + 1) };
        var affected = await UpdatePositionAsync(updated, expectedVersion, ct);
        if (affected != 1) throw new DbUpdateConcurrencyException("Paper short position changed concurrently.");
        await AddCoverAsync(new DurableShortCoverState(Guid.NewGuid(), positionId, idempotencyKey, coverPrice, coverQuantity, coveredPnl, updated.Version, now), ct);
        await transaction.CommitAsync(ct); return updated;
    }

    private async Task<DurableShortPositionState?> ReadPositionAsync(Guid positionId, bool forUpdate, CancellationToken ct)
    {
        await using var command = CreateCommand($"SELECT Id, PortfolioId, Symbol, InstrumentToken, OriginalQuantity, RemainingQuantity, AverageEntryPrice, StopLoss, TargetPrice, LastCoverPrice, RealizedPnl, State, CreatedAt, UpdatedAt, Version FROM paper_short_positions WHERE Id = @id LIMIT 1{(forUpdate ? " FOR UPDATE" : "")}");
        Add(command, "@id", positionId.ToString()); await EnsureOpenAsync(command.Connection!, ct);
        await using var reader = await command.ExecuteReaderAsync(ct); if (!await reader.ReadAsync(ct)) return null; var position = ReadPosition(reader); ValidatePositionState(position); return position;
    }

    private async Task<int> UpdatePositionAsync(DurableShortPositionState p, long expectedVersion, CancellationToken ct)
    {
        await using var command = CreateCommand("UPDATE paper_short_positions SET RemainingQuantity=@remaining, LastCoverPrice=@lastCover, RealizedPnl=@pnl, State=@state, UpdatedAt=@updated, Version=@version WHERE Id=@id AND Version=@expected");
        Add(command, "@remaining", p.RemainingQuantity); Add(command, "@lastCover", p.LastCoverPrice!.Value); Add(command, "@pnl", p.RealizedPnl); Add(command, "@state", p.State); Add(command, "@updated", p.UpdatedAt.UtcDateTime); Add(command, "@version", p.Version); Add(command, "@id", p.Id.ToString()); Add(command, "@expected", expectedVersion); return await command.ExecuteNonQueryAsync(ct);
    }

    private async Task AddCoverAsync(DurableShortCoverState c, CancellationToken ct)
    {
        await using var command = CreateCommand("INSERT INTO paper_short_covers (Id, PositionId, IdempotencyKey, CoverPrice, CoverQuantity, RealizedPnl, ResultingPositionVersion, CreatedAt) VALUES (@id,@position,@key,@price,@quantity,@pnl,@version,@created)");
        Add(command, "@id", c.Id.ToString()); Add(command, "@position", c.PositionId.ToString()); Add(command, "@key", c.IdempotencyKey); Add(command, "@price", c.CoverPrice); Add(command, "@quantity", c.CoverQuantity); Add(command, "@pnl", c.RealizedPnl); Add(command, "@version", c.ResultingPositionVersion); Add(command, "@created", c.CreatedAt.UtcDateTime); await command.ExecuteNonQueryAsync(ct);
    }

    private async Task<DurableShortCoverState?> GetCoverAsync(Guid positionId, string key, CancellationToken ct)
    {
        await using var command = CreateCommand("SELECT Id, PositionId, IdempotencyKey, CoverPrice, CoverQuantity, RealizedPnl, ResultingPositionVersion, CreatedAt FROM paper_short_covers WHERE PositionId=@position AND IdempotencyKey=@key LIMIT 1"); Add(command, "@position", positionId.ToString()); Add(command, "@key", key); await EnsureOpenAsync(command.Connection!, ct); await using var reader = await command.ExecuteReaderAsync(ct); if (!await reader.ReadAsync(ct)) return null;
        var cover = new DurableShortCoverState(ParseGuid(reader.GetValue(0)), ParseGuid(reader.GetValue(1)), reader.GetString(2), reader.GetDecimal(3), reader.GetInt32(4), reader.GetDecimal(5), reader.GetInt64(6), ReadDate(reader, 7));
        ValidateCoverState(cover, positionId);
        return cover;
    }

    private static void ValidatePositionState(DurableShortPositionState position)
    {
        if (position.Id == Guid.Empty || position.PortfolioId == Guid.Empty)
            throw new InvalidOperationException("Persisted paper short position has an invalid identity; state requires reconciliation.");
        if (string.IsNullOrWhiteSpace(position.Symbol.Value))
            throw new InvalidOperationException($"Paper short position {position.Id} has an invalid symbol; state requires reconciliation.");
        if (position.OriginalQuantity <= 0 || position.RemainingQuantity < 0 || position.RemainingQuantity > position.OriginalQuantity)
            throw new InvalidOperationException($"Paper short position {position.Id} has invalid quantities; state requires reconciliation.");
        if (position.AverageEntryPrice <= 0 || position.LastCoverPrice is <= 0 || position.Version < 0)
            throw new InvalidOperationException($"Paper short position {position.Id} has invalid price or version state; state requires reconciliation.");
        if ((position.StopLoss is null) != (position.TargetPrice is null))
            throw new InvalidOperationException($"Paper short position {position.Id} has incomplete risk levels; state requires reconciliation.");
        if (position.StopLoss is not null && !PaperShortRiskGate.Validate(position.OriginalQuantity, position.AverageEntryPrice, position.StopLoss.Value, position.TargetPrice!.Value).Approved)
            throw new InvalidOperationException($"Paper short position {position.Id} has invalid risk levels; state requires reconciliation.");
        if (position.CreatedAt == default || position.UpdatedAt < position.CreatedAt)
            throw new InvalidOperationException($"Paper short position {position.Id} has invalid timestamps; state requires reconciliation.");

        var expectedState = position.RemainingQuantity == 0
            ? "SHORT_CLOSED"
            : position.RemainingQuantity == position.OriginalQuantity ? "SHORT_OPEN" : "SHORT_PARTIALLY_COVERED";
        if (!string.Equals(position.State, expectedState, StringComparison.Ordinal))
            throw new InvalidOperationException($"Paper short position {position.Id} has inconsistent lifecycle state; state requires reconciliation.");
        if (position.Version == 0 && position.LastCoverPrice is not null)
            throw new InvalidOperationException($"Paper short position {position.Id} has a cover price without a cover version; state requires reconciliation.");
    }

    private static void ValidateCoverState(DurableShortCoverState cover, Guid positionId)
    {
        if (cover.Id == Guid.Empty || cover.PositionId != positionId || string.IsNullOrWhiteSpace(cover.IdempotencyKey))
            throw new InvalidOperationException($"Paper short cover for position {positionId} has an invalid identity; state requires reconciliation.");
        if (cover.IdempotencyKey.Length > 128 || cover.CoverPrice <= 0 || cover.CoverQuantity <= 0 || cover.ResultingPositionVersion <= 0 || cover.CreatedAt == default)
            throw new InvalidOperationException($"Paper short cover {cover.Id} has invalid persisted values; state requires reconciliation.");
    }

    private DbCommand CreateCommand(string sql) { var command = db.Database.GetDbConnection().CreateCommand(); command.CommandText = sql; command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction(); return command; }
    private static DurableShortPositionState ReadPosition(DbDataReader r) => new(ParseGuid(r.GetValue(0)), ParseGuid(r.GetValue(1)), new Symbol(r.GetString(2), r.IsDBNull(3) ? null : r.GetString(3)), r.GetInt32(4), r.GetInt32(5), r.GetDecimal(6), r.IsDBNull(7) ? null : r.GetDecimal(7), r.IsDBNull(8) ? null : r.GetDecimal(8), r.IsDBNull(9) ? null : r.GetDecimal(9), r.GetDecimal(10), r.GetString(11), ReadDate(r, 12), ReadDate(r, 13), r.GetInt64(14));
    private static Guid ParseGuid(object value) => value is Guid g ? g : Guid.Parse(Convert.ToString(value)!);
    private static DateTimeOffset ReadDate(DbDataReader r, int i) => new(DateTime.SpecifyKind(r.GetDateTime(i), DateTimeKind.Utc));
    private static void Add(DbCommand c, string name, object value) { var p=c.CreateParameter(); p.ParameterName=name; p.Value=value; c.Parameters.Add(p); }
    private static async Task EnsureOpenAsync(DbConnection c, CancellationToken ct) { if (c.State != ConnectionState.Open) await c.OpenAsync(ct); }
}
