using AiTrading.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace AiTrading.Infrastructure.Persistence.Migrations;
[DbContext(typeof(TradingDbContext))]
[Migration("202609100001_MonitoringRuns")]
public partial class MonitoringRuns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "monitoring_runs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "char(36)", nullable: false),
                StartedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                CompletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                PositionCount = table.Column<int>(nullable: false),
                FailureCount = table.Column<int>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_monitoring_runs", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_monitoring_runs_StartedAt",
            table: "monitoring_runs",
            column: "StartedAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "monitoring_runs");
    }
}
