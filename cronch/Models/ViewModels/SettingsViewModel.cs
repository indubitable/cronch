using System.ComponentModel.DataAnnotations;

namespace cronch.Models.ViewModels;

public class SettingsViewModel
{
    [Display(Name = "History page display limit")]
    [Range(10, 1000)]
    public int? MaxHistoryItemsShown { get; set; }

    [Display(Name = "Per-job execution history limit")]
    [Range(1, 10000)]
    public int? DeleteHistoricalRunsAfterCount { get; set; }

    [Display(Name = "Execution history retention (days)")]
    [Range(1, 5000)]
    public int? DeleteHistoricalRunsAfterDays { get; set; }

    [Display(Name = "Default script file location")]
    public string? DefaultScriptFileLocation { get; set; }

    [Display(Name = "Executor")]
    public string? CompletionScriptExecutor { get; set; }

    [Display(Name = "Executor arguments")]
    public string? CompletionScriptExecutorArgs { get; set; }

    [Display(Name = "Completion script")]
    public string? CompletionScript { get; set; }

    [Required(AllowEmptyStrings = false)]
    [Display(Name = "Success", Description = "Run completion script on success")]
    public bool RunCompletionScriptOnSuccess { get; set; }

    [Required(AllowEmptyStrings = false)]
    [Display(Name = "Indeterminate", Description = "Run completion script on indeterminate")]
    public bool RunCompletionScriptOnIndeterminate { get; set; }

    [Required(AllowEmptyStrings = false)]
    [Display(Name = "Warning", Description = "Run completion script on warning")]
    public bool RunCompletionScriptOnWarning { get; set; }

    [Required(AllowEmptyStrings = false)]
    [Display(Name = "Error", Description = "Run completion script on error")]
    public bool RunCompletionScriptOnError { get; set; }
}
