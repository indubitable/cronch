﻿using cronch.Models;

namespace cronch.Services;

public class ExecutionPersistenceService(ILogger<ExecutionPersistenceService> _logger, IConfiguration _configuration, CronchDbContext _dbContext)
{
    public readonly record struct ExecutionStatistics(int Successes, int Errors, int Warnings);

    public virtual DateTimeOffset? GetLatestExecutionForJob(Guid jobId)
    {
        return _dbContext.Executions
            .Where(e => e.JobId == jobId)
            .OrderByDescending(e => e.StartedOn)
            .Select(e => (DateTimeOffset?)e.StartedOn)
            .FirstOrDefault();
    }

    public virtual ExecutionStatistics GetExecutionStatistics(DateTimeOffset from,  DateTimeOffset to)
    {
        var statuses = _dbContext.Executions
            .Where(e => e.StartedOn > from && e.StartedOn <= to)
            .Select(e => e.Status)
            .ToList();

        return new ExecutionStatistics
        {
            Successes = statuses.Count(s => s == ExecutionModel.ExecutionStatus.CompletedAsSuccess),
            Errors = statuses.Count(s => s == ExecutionModel.ExecutionStatus.CompletedAsError),
            Warnings = statuses.Count(s => s == ExecutionModel.ExecutionStatus.CompletedAsWarning),
        };
    }

    public virtual void AddExecution(ExecutionModel execution)
    {
        try
        {
            _dbContext.Executions.Add(execution);
            _dbContext.SaveChanges();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot add execution: unexpected error");
            throw;
        }
    }

    public virtual void UpdateExecution(ExecutionModel execution)
    {
        try
        {
            _dbContext.Executions.Update(execution);
            _dbContext.SaveChanges();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot update execution: unexpected error");
            throw;
        }
    }

    public virtual string GetExecutionPathName(ExecutionModel execution, string relativeFileName, bool createParentDirectory)
    {
        var directory = Path.Combine(GetDataLocation(), execution.JobId.ToString("D"), GetJobRelativeExecutionPath(execution));
        if (createParentDirectory)
        {
            Directory.CreateDirectory(directory);
        }

        return Path.Combine(directory, relativeFileName);
    }

    private static string GetJobRelativeExecutionPath(ExecutionModel execution)
    {
        return Path.Combine(execution.StartedOn.ToString("yyyy-MM"), execution.GetExecutionName());
    }

    private string GetDataLocation()
    {
        var location = _configuration["DataLocation"];
        if (location == null)
        {
            _logger.LogError("Cannot determine location of execution data: DataLocation is not set");
            throw new InvalidOperationException();
        }
        return location;
    }
}
