using AiTrading.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace AiTrading.Infrastructure.Persistence.Migrations;
[DbContext(typeof(TradingDbContext))]
[Migration("202609090004_PaperTradingSessions")]
public partial class PaperTradingSessions : Migration
{
 protected override void Up(MigrationBuilder m)
 {
  m.CreateTable(name:"paper_trading_sessions",columns:t=>new{Id=t.Column<Guid>(type:"char(36)",nullable:false),SymbolsJson=t.Column<string>(type:"longtext",nullable:false),Interval=t.Column<string>(type:"varchar(16)",maxLength:16,nullable:false),StrategyVersion=t.Column<string>(type:"varchar(64)",maxLength:64,nullable:false),StartingCash=t.Column<decimal>(type:"decimal(20,4)",precision:20,scale:4,nullable:false),Status=t.Column<string>(type:"varchar(16)",maxLength:16,nullable:false),CreatedAt=t.Column<DateTimeOffset>(type:"datetime(6)",nullable:false),UpdatedAt=t.Column<DateTimeOffset>(type:"datetime(6)",nullable:false)},constraints:t=>t.PrimaryKey("PK_paper_trading_sessions",x=>x.Id));
  m.CreateIndex(name:"IX_paper_trading_sessions_CreatedAt",table:"paper_trading_sessions",column:"CreatedAt");
 }
 protected override void Down(MigrationBuilder m)=>m.DropTable(name:"paper_trading_sessions");
}
