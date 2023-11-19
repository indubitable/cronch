using System.ComponentModel.DataAnnotations;

namespace cronch.Models;

public class ExecutionModel
{
    [Key]
    public Guid Id { get; set; }

    public Guid JobId { get; set; }

    public string JobName { get; set; } = string.Empty;

    public DateTimeOffset StartedOn { get; set; }

    public ExecutionReason StartReason { get; set; } = ExecutionReason.Scheduled;

    public ExecutionStatus Status { get; set; } = ExecutionStatus.Unknown;

    public DateTimeOffset? CompletedOn { get; set; }

    public int? ExitCode { get; set; }

    public TerminationReason? StopReason { get; set; }

    public static ExecutionModel CreateNew(Guid jobId, string jobName, ExecutionReason startReason, ExecutionStatus status)
    {
        return new ExecutionModel
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            JobName = jobName,
            StartedOn = DateTimeOffset.UtcNow,
            StartReason = startReason,
            Status = status,
        };
    }

    public string GetExecutionName()
    {
        return $"{StartedOn:yyyy-MM-dd_HH-mm-ss.fff}_{Id:N}";
    }

    private ExecutionModel() { }
}
