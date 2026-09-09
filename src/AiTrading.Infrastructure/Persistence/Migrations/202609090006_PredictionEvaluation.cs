using AiTrading.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace AiTrading.Infrastructure.Persistence.Migrations;
[DbContext(typeof(TradingDbContext))]
[Migration("202609090006_PredictionEvaluation")]
public partial class PredictionEvaluation : Migration
{
 protected override void Up(MigrationBuilder m)
 {
  m.CreateTable(name:"predictions",columns:t=>new{Id=t.Column<Guid>(type:"char(36)",nullable:false),Symbol=t.Column<string>(type:"varchar(100)",maxLength:100,nullable:false),Horizon=t.Column<string>(type:"varchar(32)",maxLength:32,nullable:false),ExpectedReturn=t.Column<decimal>(type:"decimal(20,8)",precision:20,scale:8,nullable:false),ProbabilityPositive=t.Column<decimal>(type:"decimal(8,6)",precision:8,scale:6,nullable:false),Confidence=t.Column<decimal>(type:"decimal(8,6)",precision:8,scale:6,nullable:false),ModelVersion=t.Column<string>(type:"varchar(64)",maxLength:64,nullable:false),DataTimestamp=t.Column<DateTimeOffset>(type:"datetime(6)",nullable:false),RecordedAt=t.Column<DateTimeOffset>(type:"datetime(6)",nullable:false)},constraints:t=>t.PrimaryKey("PK_predictions",x=>x.Id));
  m.CreateTable(name:"prediction_outcomes",columns:t=>new{PredictionId=t.Column<Guid>(type:"char(36)",nullable:false),ActualReturn=t.Column<decimal>(type:"decimal(20,8)",precision:20,scale:8,nullable:false),OutcomeTimestamp=t.Column<DateTimeOffset>(type:"datetime(6)",nullable:false)},constraints:t=>t.PrimaryKey("PK_prediction_outcomes",x=>x.PredictionId).ForeignKey("predictions",x=>x.PredictionId,onDelete:ReferentialAction.Cascade));
  m.CreateIndex(name:"IX_predictions_Symbol_DataTimestamp",table:"predictions",columns:new[]{"Symbol","DataTimestamp"});
  m.CreateIndex(name:"IX_prediction_outcomes_OutcomeTimestamp",table:"prediction_outcomes",column:"OutcomeTimestamp");
 }
 protected override void Down(MigrationBuilder m){m.DropTable(name:"prediction_outcomes");m.DropTable(name:"predictions");}
}
