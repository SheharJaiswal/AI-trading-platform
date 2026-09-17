using AiTrading.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiTrading.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TradingDbContext))]
[Migration("202609170002_PersistShortRiskLevels")]
public partial class PersistShortRiskLevels : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "StopLoss",
            table: "paper_short_positions",
            type: "decimal(20,4)",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "TargetPrice",
            table: "paper_short_positions",
            type: "decimal(20,4)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "StopLoss", table: "paper_short_positions");
        migrationBuilder.DropColumn(name: "TargetPrice", table: "paper_short_positions");
    }
}
