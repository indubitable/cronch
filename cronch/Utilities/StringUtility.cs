using cronch.Models;

namespace cronch.Utilities;

public static class StringUtility
{
    public static string ToUserString(this ExecutionReason reason)
    {
        return reason switch
        {
            ExecutionReason.Scheduled => "Scheduled",
            ExecutionReason.Manual => "Manual",
            _ => "Unknown",
        };
    }

    public static string ToUserString(this ExecutionReason? reason)
    {
        return reason.HasValue ? reason.Value.ToUserString() : "Unknown";
    }

    public static string ToUserString(this TerminationReason reason)
    {
        return reason switch
        {
            TerminationReason.NoneSpecified => "Not specified",
            TerminationReason.Exited => "Exited normally",
            TerminationReason.TimedOut => "Timed out",
            TerminationReason.SkippedForParallelism => "Skipped due to parallelism limit",
            TerminationReason.UserTriggered => "Terminated by user",
            _ => "Unknown",
        };
    }

    public static string ToUserString(this TerminationReason? reason)
    {
        return reason.HasValue ? reason.Value.ToUserString() : "Unknown";
    }

    public static string ToUserString(this ExecutionStatus status)
    {
        return status switch
        {
            ExecutionStatus.Unknown => "Unknown",
            ExecutionStatus.Running => "Running",
            ExecutionStatus.CompletedAsSuccess => "Completed successfully",
            ExecutionStatus.CompletedAsIndeterminate => "Completed as indeterminate",
            ExecutionStatus.CompletedAsWarning => "Completed with warning",
            ExecutionStatus.CompletedAsError => "Completed with error",
            _ => "Unknown",
        };
    }

    public static string ToUserString(this ExecutionStatus? status)
    {
        return status.HasValue ? status.Value.ToUserString() : "Unknown";
    }
}
