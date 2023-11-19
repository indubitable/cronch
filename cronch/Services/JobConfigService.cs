﻿using cronch.Models;
using cronch.Models.Persistence;

namespace cronch.Services;

public class JobConfigService(ConfigPersistenceService _configPersistenceService, ConfigConverterService _configConverterService, JobSchedulingService _jobSchedulingService)
{
    public void CreateJob(JobModel jobModel, bool assignNewId)
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

        var newPersistenceJob = _configConverterService.ConvertToPersistence(jobModel);
        persistenceJobs.Add(newPersistenceJob);
        _configPersistenceService.Save(config);

        _jobSchedulingService.RefreshSchedules(GetAllJobs());
    }

    public void UpdateJob(JobModel jobModel)
    {
        var config = _configPersistenceService.Load() ?? new ConfigPersistenceModel();
        var persistenceJobs = config.Jobs;

        var oldPersistenceJob = persistenceJobs.SingleOrDefault(j => j.Id == jobModel.Id) ?? throw new InvalidOperationException("Cannot update nonexistent job");
        persistenceJobs.Remove(oldPersistenceJob);
        persistenceJobs.Add(_configConverterService.ConvertToPersistence(jobModel));
        _configPersistenceService.Save(config);

        _jobSchedulingService.RefreshSchedules(GetAllJobs());
    }

    public void DeleteJob(Guid id)
    {
        var config = _configPersistenceService.Load() ?? new ConfigPersistenceModel();
        var persistenceJobs = config.Jobs;

        var oldPersistenceJob = persistenceJobs.SingleOrDefault(j => j.Id == id) ?? throw new InvalidOperationException("Cannot delete nonexistent job");
        persistenceJobs.Remove(oldPersistenceJob);
        _configPersistenceService.Save(config);

        _jobSchedulingService.RefreshSchedules(GetAllJobs());
    }

    public List<JobModel> GetAllJobs()
    {
        var config = _configPersistenceService.Load();
        if (config == null)
        {
            return new List<JobModel>();
        }
        return _configConverterService.ConvertToModel(config).Jobs;
    }

	public JobModel GetJob(Guid id)
	{
		return GetAllJobs().Single(j => j.Id == id);
	}
}
