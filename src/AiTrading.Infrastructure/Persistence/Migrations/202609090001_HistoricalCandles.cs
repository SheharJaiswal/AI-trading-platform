using AiTrading.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiTrading.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TradingDbContext))]
[Migration("202609090001_HistoricalCandles")]
public partial class HistoricalCandles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "historical_candles", columns: table => new
        {
            Id = table.Column<Guid>(type: "char(36)", nullable: false),
            Symbol = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
            InstrumentToken = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
            Interval = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
            Timestamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
            Open = table.Column<decimal>(type: "decimal(20,4)", precision: 20, scale: 4, nullable: false),
            High = table.Column<decimal>(type: "decimal(20,4)", precision: 20, scale: 4, nullable: false),
            Low = table.Column<decimal>(type: "decimal(20,4)", precision: 20, scale: 4, nullable: false),
            Close = table.Column<decimal>(type: "decimal(20,4)", precision: 20, scale: 4, nullable: false),
            Volume = table.Column<long>(type: "bigint", nullable: false),
            Source = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
            ReceivedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_historical_candles", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_historical_candles_Identity", table: "historical_candles", columns: new[] { "Symbol", "InstrumentToken", "Interval", "Timestamp", "Source" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "historical_candles");
}