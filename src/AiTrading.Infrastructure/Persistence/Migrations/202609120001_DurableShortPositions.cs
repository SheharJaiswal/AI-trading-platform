using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiTrading.Infrastructure.Persistence.Migrations;

public partial class DurableShortPositions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "paper_short_positions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "char(36)", nullable: false),
                PortfolioId = table.Column<Guid>(type: "char(36)", nullable: false),
                Symbol = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                InstrumentToken = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                OriginalQuantity = table.Column<int>(nullable: false),
                RemainingQuantity = table.Column<int>(nullable: false),
                AverageEntryPrice = table.Column<decimal>(type: "decimal(20,4)", nullable: false),
                LastCoverPrice = table.Column<decimal>(type: "decimal(20,4)", nullable: true),
                RealizedPnl = table.Column<decimal>(type: "decimal(20,4)", nullable: false),
                State = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                Version = table.Column<long>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_paper_short_positions", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_paper_short_positions_PortfolioId_Symbol", table: "paper_short_positions", columns: new[] { "PortfolioId", "Symbol" });
        migrationBuilder.CreateIndex(name: "IX_paper_short_positions_Version", table: "paper_short_positions", column: "Version");

        migrationBuilder.CreateTable(
            name: "paper_short_covers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "char(36)", nullable: false),
                PositionId = table.Column<Guid>(type: "char(36)", nullable: false),
                IdempotencyKey = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                CoverPrice = table.Column<decimal>(type: "decimal(20,4)", nullable: false),
                CoverQuantity = table.Column<int>(nullable: false),
                RealizedPnl = table.Column<decimal>(type: "decimal(20,4)", nullable: false),
                ResultingPositionVersion = table.Column<long>(nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_paper_short_covers", x => x.Id);
                table.ForeignKey("FK_paper_short_covers_paper_short_positions_PositionId", x => x.PositionId, "paper_short_positions", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex(name: "IX_paper_short_covers_PositionId_IdempotencyKey", table: "paper_short_covers", columns: new[] { "PositionId", "IdempotencyKey" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "paper_short_covers");
        migrationBuilder.DropTable(name: "paper_short_positions");
    }
}
