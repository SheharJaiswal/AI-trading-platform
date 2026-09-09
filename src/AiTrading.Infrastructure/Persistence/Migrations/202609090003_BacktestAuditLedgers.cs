using AiTrading.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace AiTrading.Infrastructure.Persistence.Migrations;
[DbContext(typeof(TradingDbContext))]
[Migration("202609090003_BacktestAuditLedgers")]
public partial class BacktestAuditLedgers : Migration
{
 protected override void Up(MigrationBuilder m)
 {
  m.CreateTable(name:"backtest_trade_audits",columns:t=>new{Id=t.Column<Guid>(type:"char(36)",nullable:false),BacktestRunId=t.Column<Guid>(type:"char(36)",nullable:false),Timestamp=t.Column<DateTimeOffset>(type:"datetime(6)",nullable:false),Side=t.Column<string>(type:"varchar(16)",maxLength:16,nullable:false),Quantity=t.Column<int>(nullable:false),Price=t.Column<decimal>(type:"decimal(20,4)",precision:20,scale:4,nullable:false),Fee=t.Column<decimal>(type:"decimal(20,4)",precision:20,scale:4,nullable:false),RiskDecision=t.Column<string>(type:"varchar(32)",maxLength:32,nullable:false),RiskReason=t.Column<string>(type:"varchar(1000)",maxLength:1000,nullable:true)},constraints:t=>{t.PrimaryKey("PK_backtest_trade_audits",x=>x.Id);t.ForeignKey("FK_backtest_trade_audits_backtest_runs_BacktestRunId",x=>x.BacktestRunId,"backtest_runs","Id",onDelete:ReferentialAction.Cascade);});
  m.CreateIndex(name:"IX_backtest_trade_audits_BacktestRunId_Timestamp",table:"backtest_trade_audits",columns:new[]{"BacktestRunId","Timestamp"});
  m.CreateTable(name:"backtest_risk_audits",columns:t=>new{Id=t.Column<Guid>(type:"char(36)",nullable:false),BacktestRunId=t.Column<Guid>(type:"char(36)",nullable:false),Timestamp=t.Column<DateTimeOffset>(type:"datetime(6)",nullable:false),Decision=t.Column<string>(type:"varchar(32)",maxLength:32,nullable:false),Reason=t.Column<string>(type:"varchar(1000)",maxLength:1000,nullable:true),AvailableCash=t.Column<decimal>(type:"decimal(20,4)",precision:20,scale:4,nullable:false),RequestedQuantity=t.Column<int>(nullable:false)},constraints:t=>{t.PrimaryKey("PK_backtest_risk_audits",x=>x.Id);t.ForeignKey("FK_backtest_risk_audits_backtest_runs_BacktestRunId",x=>x.BacktestRunId,"backtest_runs","Id",onDelete:ReferentialAction.Cascade);});
  m.CreateIndex(name:"IX_backtest_risk_audits_BacktestRunId_Timestamp",table:"backtest_risk_audits",columns:new[]{"BacktestRunId","Timestamp"});
 }
 protected override void Down(MigrationBuilder m){m.DropTable(name:"backtest_risk_audits");m.DropTable(name:"backtest_trade_audits");}
}
