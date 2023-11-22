namespace cronch.Models;

public class SettingsModel
{
    public int? MaxHistoryItemsShown { get; set; }
    public int? DeleteHistoricalRunsAfterCount { get; set; }
    public int? DeleteHistoricalRunsAfterDays { get; set; }
}
