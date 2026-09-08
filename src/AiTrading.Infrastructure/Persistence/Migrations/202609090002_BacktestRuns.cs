using AiTrading.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace AiTrading.Infrastructure.Persistence.Migrations;
[DbContext(typeof(TradingDbContext))]
[Migration("202609090002_BacktestRuns")]
public partial class BacktestRuns : Migration
{
 protected override void Up(MigrationBuilder m){m.CreateTable(name:"backtest_runs",columns:t=>new{Id=t.Column<Guid>(type:"char(36)",nullable:false),Symbol=t.Column<string>(type:"varchar(100)",maxLength:100,nullable:false),Interval=t.Column<string>(type:"varchar(16)",maxLength:16,nullable:false),Start=t.Column<DateTimeOffset>(type:"datetime(6)",nullable:false),End=t.Column<DateTimeOffset>(type:"datetime(6)",nullable:false),StartingCash=t.Column<decimal>(type:"decimal(20,4)",precision:20,scale:4,nullable:false),QuantityPerTrade=t.Column<int>(nullable:false),FeeRate=t.Column<decimal>(type:"decimal(12,8)",precision:12,scale:8,nullable:false),SlippageBasisPoints=t.Column<decimal>(type:"decimal(12,4)",precision:12,scale:4,nullable:false),StrategyVersion=t.Column<string>(type:"varchar(64)",maxLength:64,nullable:false),ResultJson=t.Column<string>(type:"longtext",nullable:false),CreatedAt=t.Column<DateTimeOffset>(type:"datetime(6)",nullable:false)},constraints:t=>t.PrimaryKey("PK_backtest_runs",x=>x.Id));m.CreateIndex(name:"IX_backtest_runs_CreatedAt",table:"backtest_runs",column:"CreatedAt");}
 protected override void Down(MigrationBuilder m)=>m.DropTable(name:"backtest_runs");
}
