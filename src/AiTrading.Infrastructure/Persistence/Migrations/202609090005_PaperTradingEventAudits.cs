using AiTrading.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace AiTrading.Infrastructure.Persistence.Migrations;
[DbContext(typeof(TradingDbContext))]
[Migration("202609090005_PaperTradingEventAudits")]
public partial class PaperTradingEventAudits : Migration
{
 protected override void Up(MigrationBuilder m)
 {
  m.CreateTable(name:"paper_trading_event_audits",columns:t=>new{Id=t.Column<Guid>(type:"char(36)",nullable:false),SessionId=t.Column<Guid>(type:"char(36)",nullable:false),EventId=t.Column<string>(type:"varchar(128)",maxLength:128,nullable:false),OrderId=t.Column<Guid>(type:"char(36)",nullable:false),Symbol=t.Column<string>(type:"varchar(100)",maxLength:100,nullable:false),Quantity=t.Column<int>(nullable:false),RiskDecision=t.Column<string>(type:"varchar(32)",maxLength:32,nullable:false),RiskReason=t.Column<string>(type:"varchar(1000)",maxLength:1000,nullable:false),FillPrice=t.Column<decimal>(type:"decimal(20,4)",precision:20,scale:4,nullable:true),CreatedAt=t.Column<DateTimeOffset>(type:"datetime(6)",nullable:false)},constraints:t=>t.PrimaryKey("PK_paper_trading_event_audits",x=>x.Id));
  m.CreateIndex(name:"IX_paper_trading_event_audits_SessionId_CreatedAt",table:"paper_trading_event_audits",columns:new[]{"SessionId","CreatedAt"});
  m.CreateIndex(name:"IX_paper_trading_event_audits_SessionId_EventId",table:"paper_trading_event_audits",columns:new[]{"SessionId","EventId"},unique:true);
 }
 protected override void Down(MigrationBuilder m)=>m.DropTable(name:"paper_trading_event_audits");
}
