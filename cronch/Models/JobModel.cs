namespace cronch.Models;

public class JobModel
{
    public enum OutputProcessing
    {
        None,
        WarningOnAnyOutput,
        ErrorOnAnyOutput,
        WarningOnMatchingKeywords,
        ErrorOnMatchingKeywords
    }

    public enum ParallelSkipProcessing
    {
        Ignore,
        MarkAsIndeterminate,
        MarkAsWarning,
        MarkAsError
    }

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    public string CronSchedule { get; set; } = string.Empty;

    public string Executor { get; set; } = string.Empty;

    public string? ExecutorArgs { get; set; }

    public string Script { get; set; } = string.Empty;

    public string? ScriptFilePathname { get; set; }

    public double? TimeLimitSecs { get; set; }

    public int? Parallelism { get; set; }

    public ParallelSkipProcessing MarkParallelSkipAs { get; set; } = ParallelSkipProcessing.Ignore;

    public List<string> Keywords { get; set; } = [];

    public OutputProcessing StdOutProcessing { get; set; } = OutputProcessing.None;

    public OutputProcessing StdErrProcessing { get; set; } = OutputProcessing.None;
}
