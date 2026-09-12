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

    public async Task AddAsync(DurableShortPositionState position, CancellationToken ct)
    {
        await using var command = CreateCommand("INSERT INTO paper_short_positions (Id, PortfolioId, Symbol, InstrumentToken, OriginalQuantity, RemainingQuantity, AverageEntryPrice, LastCoverPrice, RealizedPnl, State, CreatedAt, UpdatedAt, Version) VALUES (@id,@portfolio,@symbol,@token,@original,@remaining,@entry,@lastCover,@pnl,@state,@created,@updated,@version)");
        Add(command, "@id", position.Id.ToString()); Add(command, "@portfolio", position.PortfolioId.ToString()); Add(command, "@symbol", position.Symbol.Value); Add(command, "@token", (object?)position.Symbol.InstrumentToken ?? DBNull.Value); Add(command, "@original", position.OriginalQuantity); Add(command, "@remaining", position.RemainingQuantity); Add(command, "@entry", position.AverageEntryPrice); Add(command, "@lastCover", position.LastCoverPrice is null ? DBNull.Value : position.LastCoverPrice.Value); Add(command, "@pnl", position.RealizedPnl); Add(command, "@state", position.State); Add(command, "@created", position.CreatedAt.UtcDateTime); Add(command, "@updated", position.UpdatedAt.UtcDateTime); Add(command, "@version", position.Version);
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
        await using var command = CreateCommand($"SELECT Id, PortfolioId, Symbol, InstrumentToken, OriginalQuantity, RemainingQuantity, AverageEntryPrice, LastCoverPrice, RealizedPnl, State, CreatedAt, UpdatedAt, Version FROM paper_short_positions WHERE Id = @id LIMIT 1{(forUpdate ? " FOR UPDATE" : "")}");
        Add(command, "@id", positionId.ToString()); await EnsureOpenAsync(command.Connection!, ct);
        await using var reader = await command.ExecuteReaderAsync(ct); if (!await reader.ReadAsync(ct)) return null; return ReadPosition(reader);
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
        return new DurableShortCoverState(ParseGuid(reader.GetValue(0)), ParseGuid(reader.GetValue(1)), reader.GetString(2), reader.GetDecimal(3), reader.GetInt32(4), reader.GetDecimal(5), reader.GetInt64(6), ReadDate(reader, 7));
    }

    private DbCommand CreateCommand(string sql) { var command = db.Database.GetDbConnection().CreateCommand(); command.CommandText = sql; command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction(); return command; }
    private static DurableShortPositionState ReadPosition(DbDataReader r) => new(ParseGuid(r.GetValue(0)), ParseGuid(r.GetValue(1)), new Symbol(r.GetString(2), r.IsDBNull(3) ? null : r.GetString(3)), r.GetInt32(4), r.GetInt32(5), r.GetDecimal(6), r.IsDBNull(7) ? null : r.GetDecimal(7), r.GetDecimal(8), r.GetString(9), ReadDate(r, 10), ReadDate(r, 11), r.GetInt64(12));
    private static Guid ParseGuid(object value) => value is Guid g ? g : Guid.Parse(Convert.ToString(value)!);
    private static DateTimeOffset ReadDate(DbDataReader r, int i) => new(DateTime.SpecifyKind(r.GetDateTime(i), DateTimeKind.Utc));
    private static void Add(DbCommand c, string name, object value) { var p=c.CreateParameter(); p.ParameterName=name; p.Value=value; c.Parameters.Add(p); }
    private static async Task EnsureOpenAsync(DbConnection c, CancellationToken ct) { if (c.State != ConnectionState.Open) await c.OpenAsync(ct); }
}
