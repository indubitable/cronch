using cronch.Models;
using cronch.Models.Persistence;
using cronch.Models.ViewModels;

namespace cronch.Services;

public class ConfigConverterService
{
    public ConfigModel ConvertToModel(ConfigPersistenceModel configPersistenceModel)
    {
        return new ConfigModel
        {
            Jobs = configPersistenceModel.Jobs.Select(ConvertToModel).ToList()
        };
    }

    public JobModel ConvertToModel(JobPersistenceModel jobPersistenceModel)
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
            MarkParallelSkipAs = ParseEnumValueWithFallback(jobPersistenceModel.MarkParallelSkipAs, JobModel.ParallelSkipProcessing.Ignore),
            Keywords = new List<string>(jobPersistenceModel.Keywords),
            StdOutProcessing = ParseEnumValueWithFallback(jobPersistenceModel.StdOutProcessing, JobModel.OutputProcessing.None),
            StdErrProcessing = ParseEnumValueWithFallback(jobPersistenceModel.StdErrProcessing, JobModel.OutputProcessing.None),
        };
    }

    public JobModel ConvertToModel(JobViewModel jobViewModel)
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
            MarkParallelSkipAs = ParseEnumValueWithFallback(jobViewModel.MarkParallelSkipAs, JobModel.ParallelSkipProcessing.Ignore),
            Keywords = (jobViewModel.Keywords ?? "").Split(',').Select(s => s.Trim()).ToList(),
            StdOutProcessing = ParseEnumValueWithFallback(jobViewModel.StdOutProcessing, JobModel.OutputProcessing.None),
            StdErrProcessing = ParseEnumValueWithFallback(jobViewModel.StdErrProcessing, JobModel.OutputProcessing.None),
        };
    }

    public ConfigPersistenceModel ConvertToPersistence(ConfigModel configModel)
    {
        return new ConfigPersistenceModel
        {
            Jobs = configModel.Jobs.Select(ConvertToPersistence).ToList()
        };
    }

    public JobPersistenceModel ConvertToPersistence(JobModel jobModel)
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
        };
    }

    public JobViewModel ConvertToViewModel(JobModel jobModel)
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
        };
    }

    private static T ParseEnumValueWithFallback<T>(string value, T fallback) where T : Enum
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
