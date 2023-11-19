namespace cronch.Models;

public enum TerminationReason
{
    NoneSpecified,
    Exited,
    TimedOut,
    SkippedForParallelism
}
