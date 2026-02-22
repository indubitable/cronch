using cronch.Models.Persistence;
using cronch.Models;
using cronch.Models.ViewModels;

namespace cronch.Utilities;

public static class ConversionUtility
{
    public static JobModel ToModel(this JobPersistenceModel jobPersistenceModel)
    {
        return new JobModel
        {
            Id = jobPersistenceModel.Id,
            Name = jobPersistenceModel.Name,
            Enabled = jobPersistenceModel.Enabled,
            CronSchedule = jobPersistenceModel.CronSchedule,
            Executor = jobPersistenceModel.Executor,
            ExecutorArgs = jobPersistenceModel.ExecutorArgs,
            Script = jobPersistenceModel.Script,
            ScriptFilePathname = jobPersistenceModel.ScriptFilePathname,
            TimeLimitSecs = jobPersistenceModel.TimeLimitSecs,
            Parallelism = jobPersistenceModel.Parallelism,
            MarkParallelSkipAs = jobPersistenceModel.MarkParallelSkipAs.ToEnumWithFallback(JobModel.ParallelSkipProcessing.Ignore),
            Keywords = new List<string>(jobPersistenceModel.Keywords),
            StdOutProcessing = jobPersistenceModel.StdOutProcessing.ToEnumWithFallback(JobModel.OutputProcessing.None),
            StdErrProcessing = jobPersistenceModel.StdErrProcessing.ToEnumWithFallback(JobModel.OutputProcessing.None),
            ChainRules = jobPersistenceModel.ChainRules.Select(r => new ChainRuleModel
            {
                TargetJobId = r.TargetJobId,
                RunOnSuccess = r.RunOnSuccess,
                RunOnIndeterminate = r.RunOnIndeterminate,
                RunOnWarning = r.RunOnWarning,
                RunOnError = r.RunOnError,
            }).ToList(),
        };
    }

    public static JobModel ToModel(this JobViewModel jobViewModel)
    {
        return new JobModel
        {
            Id = jobViewModel.Id,
            Name = jobViewModel.Name,
            Enabled = jobViewModel.Enabled,
            CronSchedule = jobViewModel.CronSchedule,
            Executor = jobViewModel.Executor,
            ExecutorArgs = jobViewModel.ExecutorArgs,
            Script = jobViewModel.Script,
            ScriptFilePathname = jobViewModel.ScriptFilePathname,
            TimeLimitSecs = jobViewModel.TimeLimitSecs,
            Parallelism = jobViewModel.Parallelism,
            MarkParallelSkipAs = jobViewModel.MarkParallelSkipAs.ToEnumWithFallback(JobModel.ParallelSkipProcessing.Ignore),
            Keywords = (jobViewModel.Keywords ?? "").Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList(),
            StdOutProcessing = jobViewModel.StdOutProcessing.ToEnumWithFallback(JobModel.OutputProcessing.None),
            StdErrProcessing = jobViewModel.StdErrProcessing.ToEnumWithFallback(JobModel.OutputProcessing.None),
            ChainRules = jobViewModel.ChainRules.Select(r => new ChainRuleModel
            {
                TargetJobId = r.TargetJobId,
                RunOnSuccess = r.RunOnSuccess,
                RunOnIndeterminate = r.RunOnIndeterminate,
                RunOnWarning = r.RunOnWarning,
                RunOnError = r.RunOnError,
            }).ToList(),
        };
    }

    public static SettingsModel ToModel(this SettingsViewModel settingsViewModel)
    {
        return new SettingsModel
        {
            MaxHistoryItemsShown = settingsViewModel.MaxHistoryItemsShown,
            DeleteHistoricalRunsAfterCount = settingsViewModel.DeleteHistoricalRunsAfterCount,
            DeleteHistoricalRunsAfterDays = settingsViewModel.DeleteHistoricalRunsAfterDays,
            DefaultScriptFileLocation = settingsViewModel.DefaultScriptFileLocation,
            CompletionScriptExecutor = settingsViewModel.CompletionScriptExecutor,
            CompletionScriptExecutorArgs = settingsViewModel.CompletionScriptExecutorArgs,
            CompletionScript = settingsViewModel.CompletionScript,
            RunCompletionScriptOnSuccess = settingsViewModel.RunCompletionScriptOnSuccess,
            RunCompletionScriptOnIndeterminate = settingsViewModel.RunCompletionScriptOnIndeterminate,
            RunCompletionScriptOnWarning = settingsViewModel.RunCompletionScriptOnWarning,
            RunCompletionScriptOnError = settingsViewModel.RunCompletionScriptOnError,
            MaxChainDepth = settingsViewModel.MaxChainDepth,
        };
    }

    public static JobPersistenceModel ToPersistence(this JobModel jobModel)
    {
        return new JobPersistenceModel
        {
            Id = jobModel.Id,
            Name = jobModel.Name,
            Enabled = jobModel.Enabled,
            CronSchedule = jobModel.CronSchedule,
            Executor = jobModel.Executor,
            ExecutorArgs = jobModel.ExecutorArgs,
            Script = jobModel.Script,
            ScriptFilePathname = jobModel.ScriptFilePathname,
            TimeLimitSecs = jobModel.TimeLimitSecs,
            Parallelism = jobModel.Parallelism,
            MarkParallelSkipAs = jobModel.MarkParallelSkipAs.ToString(),
            Keywords = new List<string>(jobModel.Keywords),
            StdOutProcessing = jobModel.StdOutProcessing.ToString(),
            StdErrProcessing = jobModel.StdErrProcessing.ToString(),
            ChainRules = jobModel.ChainRules.Select(r => new ChainRulePersistenceModel
            {
                TargetJobId = r.TargetJobId,
                RunOnSuccess = r.RunOnSuccess,
                RunOnIndeterminate = r.RunOnIndeterminate,
                RunOnWarning = r.RunOnWarning,
                RunOnError = r.RunOnError,
            }).ToList(),
        };
    }

    public static JobViewModel ToViewModel(this JobModel jobModel)
    {
        return new JobViewModel
        {
            Id = jobModel.Id,
            Name = jobModel.Name,
            Enabled = jobModel.Enabled,
            CronSchedule = jobModel.CronSchedule,
            Executor = jobModel.Executor,
            ExecutorArgs = jobModel.ExecutorArgs,
            Script = jobModel.Script,
            ScriptFilePathname = jobModel.ScriptFilePathname,
            TimeLimitSecs = jobModel.TimeLimitSecs,
            Parallelism = jobModel.Parallelism,
            MarkParallelSkipAs = jobModel.MarkParallelSkipAs.ToString(),
            Keywords = string.Join(',', jobModel.Keywords),
            StdOutProcessing = jobModel.StdOutProcessing.ToString(),
            StdErrProcessing = jobModel.StdErrProcessing.ToString(),
            ChainRules = jobModel.ChainRules.Select(r => new ChainRuleViewModel
            {
                TargetJobId = r.TargetJobId,
                RunOnSuccess = r.RunOnSuccess,
                RunOnIndeterminate = r.RunOnIndeterminate,
                RunOnWarning = r.RunOnWarning,
                RunOnError = r.RunOnError,
            }).ToList(),
        };
    }

    public static SettingsViewModel ToViewModel(this SettingsModel settingsModel)
    {
        return new SettingsViewModel
        {
            MaxHistoryItemsShown = settingsModel.MaxHistoryItemsShown,
            DeleteHistoricalRunsAfterCount = settingsModel.DeleteHistoricalRunsAfterCount,
            DeleteHistoricalRunsAfterDays = settingsModel.DeleteHistoricalRunsAfterDays,
            DefaultScriptFileLocation = settingsModel.DefaultScriptFileLocation,
            CompletionScriptExecutor = settingsModel.CompletionScriptExecutor,
            CompletionScriptExecutorArgs = settingsModel.CompletionScriptExecutorArgs,
            CompletionScript = settingsModel.CompletionScript,
            RunCompletionScriptOnSuccess = settingsModel.RunCompletionScriptOnSuccess,
            RunCompletionScriptOnIndeterminate = settingsModel.RunCompletionScriptOnIndeterminate,
            RunCompletionScriptOnWarning = settingsModel.RunCompletionScriptOnWarning,
            RunCompletionScriptOnError = settingsModel.RunCompletionScriptOnError,
            MaxChainDepth = settingsModel.MaxChainDepth,
        };
    }

    public static T ToEnumWithFallback<T>(this string value, T fallback) where T : Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        if (Enum.TryParse(typeof(T), value, out var result))
        {
            return (T)result;
        }

        return fallback;
    }
}
