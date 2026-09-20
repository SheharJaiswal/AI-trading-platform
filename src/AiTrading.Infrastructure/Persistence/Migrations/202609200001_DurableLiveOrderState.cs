using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiTrading.Infrastructure.Persistence.Migrations;

public partial class DurableLiveOrderState : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "live_order_states",
            columns: table => new
            {
                OrderId = table.Column<Guid>(type: "char(36)", nullable: false),
                IdempotencyKey = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                AccountContext = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                Provider = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                ProviderOrderId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                ReconciliationRequired = table.Column<bool>(nullable: false),
                Reason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                Version = table.Column<long>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_live_order_states", x => x.OrderId));

        migrationBuilder.CreateIndex(
            name: "IX_live_order_states_IdempotencyKey",
            table: "live_order_states",
            column: "IdempotencyKey",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "live_order_states");
    }
}
