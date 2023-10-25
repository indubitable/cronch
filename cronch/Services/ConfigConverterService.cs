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
            Script = jobPersistenceModel.Script,
            ScriptFilePathname = jobPersistenceModel.ScriptFilePathname,
            Keywords = new List<string>(jobPersistenceModel.Keywords),
            StdOutProcessing = Enum.Parse<JobModel.OutputProcessing>(jobPersistenceModel.StdOutProcessing),
            StdErrProcessing = Enum.Parse<JobModel.OutputProcessing>(jobPersistenceModel.StdErrProcessing),
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
            Script = jobViewModel.Script,
            ScriptFilePathname = jobViewModel.ScriptFilePathname,
            Keywords = (jobViewModel.Keywords ?? "").Split(',').Select(s => s.Trim()).ToList(),
            StdOutProcessing = Enum.Parse<JobModel.OutputProcessing>(jobViewModel.StdOutProcessing),
            StdErrProcessing = Enum.Parse<JobModel.OutputProcessing>(jobViewModel.StdErrProcessing),
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
            Script = jobModel.Script,
            ScriptFilePathname = jobModel.ScriptFilePathname,
            Keywords = new List<string>(jobModel.Keywords),
            StdOutProcessing = jobModel.StdOutProcessing.ToString(),
            StdErrProcessing = jobModel.StdErrProcessing.ToString(),
        };
    }
}
