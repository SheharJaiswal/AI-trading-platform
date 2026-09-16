using AiTrading.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiTrading.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TradingDbContext))]
[Migration("202609170001_PortfolioPeakEquity")]
public partial class PortfolioPeakEquity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "PeakEquity",
            table: "portfolios",
            type: "decimal(20,4)",
            nullable: false,
            defaultValue: 0m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PeakEquity",
            table: "portfolios");
    }
}
