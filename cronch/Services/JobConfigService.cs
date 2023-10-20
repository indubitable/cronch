using cronch.Models;
using cronch.Models.Persistence;

namespace cronch.Services;

public class JobConfigService
{
    private readonly ConfigPersistenceService _configPersistenceService;
    private readonly ConfigConverterService _configConverterService;

    public JobConfigService(ConfigPersistenceService configPersistenceService, ConfigConverterService configConverterService)
    {
        _configPersistenceService = configPersistenceService;
        _configConverterService = configConverterService;
    }

    public void CreateJob()
    {

    }

    public void UpdateJob()
    {

    }

    public void DeleteJob()
    {

    }

    public List<JobModel> GetAllJobs()
    {
        var config = _configPersistenceService.Load();
        if (config == null)
        {
            return new List<JobModel>();
        }
        return _configConverterService.ConvertFromPersistence(config).Jobs;
    }
}
