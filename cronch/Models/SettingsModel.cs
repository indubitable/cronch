namespace cronch.Models;

public class SettingsModel
{
    public int? MaxHistoryItemsShown { get; set; }
    public int? DeleteHistoricalRunsAfterCount { get; set; }
    public int? DeleteHistoricalRunsAfterDays { get; set; }
    public string? CompletionScriptExecutor { get; set; }
    public string? CompletionScriptExecutorArgs { get; set; }
    public string? CompletionScript { get; set; }
    public bool RunCompletionScriptOnSuccess { get; set; }
    public bool RunCompletionScriptOnIndeterminate { get; set; }
    public bool RunCompletionScriptOnWarning { get; set; }
    public bool RunCompletionScriptOnError { get; set; }
    public bool MakeOutputAvailableToScript { get; set; }
}
