using cronch.Models;
using cronch.Models.Persistence;

namespace cronch.Services;

public class SettingsService(ConfigPersistenceService _configPersistenceService)
{
    public const int DefaultMaxHistoryItemsShown = 250;

    public void SaveSettings(SettingsModel settingsModel)
    {
        var persistenceModel = _configPersistenceService.Load() ?? new ConfigPersistenceModel();
        persistenceModel.MaxHistoryItemsShown = settingsModel.MaxHistoryItemsShown;
        persistenceModel.DeleteHistoricalRunsAfterCount = settingsModel.DeleteHistoricalRunsAfterCount;
        persistenceModel.DeleteHistoricalRunsAfterDays = settingsModel.DeleteHistoricalRunsAfterDays;
        _configPersistenceService.Save(persistenceModel);
    }

    public SettingsModel LoadSettings()
    {
        var persistenceModel = _configPersistenceService.Load() ?? new ConfigPersistenceModel();
        return new SettingsModel
        {
            MaxHistoryItemsShown = persistenceModel.MaxHistoryItemsShown,
            DeleteHistoricalRunsAfterCount = persistenceModel.DeleteHistoricalRunsAfterCount,
            DeleteHistoricalRunsAfterDays = persistenceModel.DeleteHistoricalRunsAfterDays,
        };
    }
}
