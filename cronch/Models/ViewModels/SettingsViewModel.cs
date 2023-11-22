using System.ComponentModel.DataAnnotations;

namespace cronch.Models.ViewModels;

public class SettingsViewModel
{
    [Display(Name = "Maximum number of runs to show in History")]
    [Range(10, 1000)]
    public int? MaxHistoryItemsShown { get; set; }

    [Display(Name = "Maximum count of historical runs")]
    [Range(1, 10000)]
    public int? DeleteHistoricalRunsAfterCount { get; set; }

    [Display(Name = "Mmaximum age of historical runs")]
    [Range(1, 5000)]
    public int? DeleteHistoricalRunsAfterDays { get; set; }
}
