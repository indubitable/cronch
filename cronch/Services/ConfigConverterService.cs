using cronch.Models;
using cronch.Models.Persistence;

namespace cronch.Services;

public class ConfigConverterService
{
    public ConfigModel ConvertFromPersistence(ConfigPersistenceModel persistenceModel)
    {
        return new ConfigModel
        {
            Jobs = persistenceModel.Jobs.Select(j => new JobModel
            {
                Id = j.Id,
                Name = j.Name,
                Enabled = j.Enabled,
                CronSchedule = j.CronSchedule,
                Executor = j.Executor,
                Script = j.Script,
                ScriptFilePathname = j.ScriptFilePathname,
                Keywords = new List<string>(j.Keywords),
                StdOutProcessing = Enum.Parse<JobModel.OutputProcessing>(j.StdOutProcessing),
                StdErrProcessing = Enum.Parse<JobModel.OutputProcessing>(j.StdErrProcessing),
            }).ToList()
        };
    }

    public ConfigPersistenceModel ConvertToPersistence(ConfigModel configModel)
    {
        return new ConfigPersistenceModel
        {
            Jobs = configModel.Jobs.Select(j => new JobPersistenceModel
            {
                Id = j.Id,
                Name = j.Name,
                Enabled = j.Enabled,
                CronSchedule = j.CronSchedule,
                Executor = j.Executor,
                Script = j.Script,
                ScriptFilePathname = j.ScriptFilePathname,
                Keywords = new List<string>(j.Keywords),
                StdOutProcessing = j.StdOutProcessing.ToString(),
                StdErrProcessing = j.StdErrProcessing.ToString(),
            }).ToList()
        };
    }
}
