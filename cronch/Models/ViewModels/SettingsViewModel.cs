using System.ComponentModel.DataAnnotations;

namespace cronch.Models.ViewModels;

public class SettingsViewModel
{
    [Display(Name = "Maximum number of run executions to show in History")]
    [Range(10, 1000)]
    public int? MaxHistoryItemsShown { get; set; }

    [Display(Name = "Maximum count of historical executions per run")]
    [Range(1, 10000)]
    public int? DeleteHistoricalRunsAfterCount { get; set; }

    [Display(Name = "Maximum age of historical run executions in days")]
    [Range(1, 5000)]
    public int? DeleteHistoricalRunsAfterDays { get; set; }

    [Display(Name = "Run completion script executor")]
    public string? CompletionScriptExecutor { get; set; }

    [Display(Name = "Run completion script executor arguments")]
    public string? CompletionScriptExecutorArgs { get; set; }

    [Display(Name = "Run completion script")]
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

    [Required(AllowEmptyStrings = false)]
    [Display(Name = "Make job output available to script")]
    public bool MakeOutputAvailableToScript { get; set; }
}
