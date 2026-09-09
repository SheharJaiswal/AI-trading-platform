using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AiTrading.Application;

namespace AiTrading.Infrastructure.Persistence;

[Table("monitoring_runs")]
public sealed class MonitoringRunRecord
{
    [Key]
    [Column(TypeName = "char(36)")]
    public Guid Id { get; set; }

    [Column(TypeName = "datetime(6)")]
    public DateTimeOffset StartedAt { get; set; }

    [Column(TypeName = "datetime(6)")]
    public DateTimeOffset? CompletedAt { get; set; }

    [Required, MaxLength(32)]
    public string Status { get; set; } = MonitoringRunStatus.Running.ToString();

    public int PositionCount { get; set; }
    public int FailureCount { get; set; }
}
