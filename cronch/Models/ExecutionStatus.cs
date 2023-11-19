namespace cronch.Models;

public enum ExecutionStatus
{
    Unknown,
    Running,
    CompletedAsSuccess,
    CompletedAsIndeterminate,
    CompletedAsWarning,
    CompletedAsError,
}
