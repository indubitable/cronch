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
}
