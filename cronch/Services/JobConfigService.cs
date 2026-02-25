using cronch.Models;
using cronch.Models.Persistence;
using cronch.Utilities;

namespace cronch.Services;

public class JobConfigService(ConfigPersistenceService _configPersistenceService, JobSchedulingService _jobSchedulingService)
{
    public async Task CreateJobAsync(JobModel jobModel, bool assignNewId)
    {
        var config = _configPersistenceService.Load() ?? new ConfigPersistenceModel();
        var persistenceJobs = config.Jobs;

        if (assignNewId)
        {
            jobModel.Id = Guid.NewGuid();
        }
        else if (persistenceJobs.Any(j => j.Id == jobModel.Id))
        {
            throw new InvalidOperationException("Cannot create job with duplicate Id");
        }

        var newPersistenceJob = jobModel.ToPersistence();
        persistenceJobs.Add(newPersistenceJob);
        _configPersistenceService.Save(config);

        await _jobSchedulingService.RefreshSchedulesAsync(GetAllJobs());
    }

    public async Task UpdateJobAsync(JobModel jobModel)
    {
        var config = _configPersistenceService.Load() ?? new ConfigPersistenceModel();
        var persistenceJobs = config.Jobs;

        var oldPersistenceJob = persistenceJobs.SingleOrDefault(j => j.Id == jobModel.Id) ?? throw new InvalidOperationException("Cannot update nonexistent job");
        persistenceJobs.Remove(oldPersistenceJob);
        persistenceJobs.Add(jobModel.ToPersistence());
        _configPersistenceService.Save(config);

        await _jobSchedulingService.RefreshSchedulesAsync(GetAllJobs());
    }

    public async Task DeleteJobAsync(Guid id)
    {
        var config = _configPersistenceService.Load() ?? new ConfigPersistenceModel();
        var persistenceJobs = config.Jobs;

        var oldPersistenceJob = persistenceJobs.SingleOrDefault(j => j.Id == id) ?? throw new InvalidOperationException("Cannot delete nonexistent job");
        persistenceJobs.Remove(oldPersistenceJob);
        _configPersistenceService.Save(config);

        await _jobSchedulingService.RefreshSchedulesAsync(GetAllJobs());
    }

    public virtual List<JobModel> GetAllJobs()
    {
        var config = _configPersistenceService.Load();
        if (config == null)
        {
            return [];
        }
        return config.Jobs.Select(ConversionUtility.ToModel).ToList();
    }

    public virtual JobModel? GetJob(Guid id) => GetAllJobs().SingleOrDefault(j => j.Id == id);
}
