using cronch.Models;
using cronch.Models.Persistence;

namespace cronch.Services;

public class SettingsService(ConfigPersistenceService _configPersistenceService)
{
    public const int DefaultMaxHistoryItemsShown = 250;
    public const int MaxCompletionScriptRuntimeSeconds = 20;

    public virtual void SaveSettings(SettingsModel settingsModel)
    {
        var persistenceModel = _configPersistenceService.Load() ?? new ConfigPersistenceModel();
        ApplySettingsToPersistenceModel(settingsModel, persistenceModel);
        _configPersistenceService.Save(persistenceModel);
    }

    public virtual SettingsModel LoadSettings()
    {
        var persistenceModel = _configPersistenceService.Load() ?? new ConfigPersistenceModel();
        return CreateSettingsModelFromPersistenceModel(persistenceModel);
    }

    private static void ApplySettingsToPersistenceModel(SettingsModel settingsModel, ConfigPersistenceModel persistenceModel)
    {
        persistenceModel.MaxHistoryItemsShown = settingsModel.MaxHistoryItemsShown;
        persistenceModel.DeleteHistoricalRunsAfterCount = settingsModel.DeleteHistoricalRunsAfterCount;
        persistenceModel.DeleteHistoricalRunsAfterDays = settingsModel.DeleteHistoricalRunsAfterDays;
        persistenceModel.DefaultScriptFileLocation = settingsModel.DefaultScriptFileLocation;

        persistenceModel.CompletionScriptExecutor = settingsModel.CompletionScriptExecutor;
        persistenceModel.CompletionScriptExecutorArgs = settingsModel.CompletionScriptExecutorArgs;
        persistenceModel.CompletionScript = settingsModel.CompletionScript;

        persistenceModel.RunCompletionScriptOn = [];
        if (settingsModel.RunCompletionScriptOnSuccess) persistenceModel.RunCompletionScriptOn.Add("Success");
        if (settingsModel.RunCompletionScriptOnIndeterminate) persistenceModel.RunCompletionScriptOn.Add("Indeterminate");
        if (settingsModel.RunCompletionScriptOnWarning) persistenceModel.RunCompletionScriptOn.Add("Warning");
        if (settingsModel.RunCompletionScriptOnError) persistenceModel.RunCompletionScriptOn.Add("Error");

        persistenceModel.MaxChainDepth = settingsModel.MaxChainDepth;
    }

    private static SettingsModel CreateSettingsModelFromPersistenceModel(ConfigPersistenceModel persistenceModel)
    {
        return new SettingsModel
        {
            MaxHistoryItemsShown = persistenceModel.MaxHistoryItemsShown,
            DeleteHistoricalRunsAfterCount = persistenceModel.DeleteHistoricalRunsAfterCount,
            DeleteHistoricalRunsAfterDays = persistenceModel.DeleteHistoricalRunsAfterDays,
            DefaultScriptFileLocation = persistenceModel.DefaultScriptFileLocation,

            CompletionScriptExecutor = persistenceModel.CompletionScriptExecutor,
            CompletionScriptExecutorArgs = persistenceModel.CompletionScriptExecutorArgs,
            CompletionScript = persistenceModel.CompletionScript,

            RunCompletionScriptOnSuccess = persistenceModel.RunCompletionScriptOn.Contains("Success"),
            RunCompletionScriptOnIndeterminate = persistenceModel.RunCompletionScriptOn.Contains("Indeterminate"),
            RunCompletionScriptOnWarning = persistenceModel.RunCompletionScriptOn.Contains("Warning"),
            RunCompletionScriptOnError = persistenceModel.RunCompletionScriptOn.Contains("Error"),
            MaxChainDepth = persistenceModel.MaxChainDepth,
        };
    }
}
