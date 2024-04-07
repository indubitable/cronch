﻿using cronch.Models;
using Microsoft.EntityFrameworkCore;

namespace cronch.Services;

public class ExecutionPersistenceService(ILogger<ExecutionPersistenceService> _logger, IConfiguration _configuration, CronchDbContext _dbContext)
{
    public readonly record struct ExecutionStatistics(int Successes, int Errors, int Warnings);

    public virtual Dictionary<Guid, DateTimeOffset> GetLatestExecutionsPerJob()
    {
        return _dbContext.Executions
            .FromSqlRaw(@"SELECT t1.* FROM Execution t1 JOIN (SELECT JobId, MAX(StartedOn) AS StartedOn FROM Execution GROUP BY JobId) t2 ON t1.JobId=t2.JobId AND t1.StartedOn=t2.StartedOn")
            .AsNoTracking()
            .Select(e => KeyValuePair.Create(e.JobId, e.StartedOn))
            .ToDictionary();
    }

    public virtual ExecutionStatistics GetExecutionStatistics(DateTimeOffset from,  DateTimeOffset to)
    {
        var statuses = _dbContext.Executions
            .AsNoTracking()
            .Where(e => e.StartedOn > from && e.StartedOn <= to)
            .Select(e => e.Status)
            .ToList();

        return new ExecutionStatistics
        {
            Successes = statuses.Count(s => s == ExecutionStatus.CompletedAsSuccess),
            Errors = statuses.Count(s => s == ExecutionStatus.CompletedAsError),
            Warnings = statuses.Count(s => s == ExecutionStatus.CompletedAsWarning),
        };
    }

    public virtual ExecutionModel? GetExecution(Guid id)
    {
        return _dbContext.Executions
            .AsNoTracking()
            .Where(e => e.Id == id)
            .SingleOrDefault();
    }

    public virtual List<ExecutionModel> GetRecentExecutions(int maxCount, Guid? jobId)
    {
        return _dbContext.Executions
            .AsNoTracking()
            .Where(e => (!jobId.HasValue || e.JobId == jobId))
            .OrderByDescending(e => e.StartedOn)
            .Take(maxCount)
            .ToList();
    }

    public virtual void AddExecution(ExecutionModel execution)
    {
        try
        {
            WriteWithRetry(() =>
            {
                _dbContext.Executions.Add(execution);
                _dbContext.SaveChanges();
            });
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
            WriteWithRetry(() =>
            {
                _dbContext.Executions.Update(execution);
                _dbContext.SaveChanges();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot update execution: unexpected error");
            throw;
        }
    }

    public virtual string GetOutputPathName(ExecutionModel execution, bool createParentDirectory)
    {
        var directory = Path.Combine(GetDataLocation(), execution.JobId.ToString("D"), execution.StartedOn.ToString("yyyy-MM"));
        if (createParentDirectory)
        {
            Directory.CreateDirectory(directory);
        }

        return Path.Combine(directory, execution.GetExecutionName() + ".txt");
    }

    public virtual List<string> GetAllDataSubdirectories()
    {
        return Directory.EnumerateDirectories(GetDataLocation())
            .Select(d => d.EndsWith(Path.DirectorySeparatorChar) ? d[0..^1] : d)
            .Where(d => Guid.TryParseExact(Path.GetFileName(d), "D", out var _))
            .ToList();
    }

    public virtual void RemoveAllDatabaseRecordsForJobId(Guid id)
    {
        WriteWithRetry(() =>
        {
            _dbContext.Executions
                .Where(e => e.JobId == id)
                .ExecuteDelete();
        });
    }

    public List<ExecutionModel> GetOldestExecutionsAfterCount(Guid jobId, int skipCount)
    {
        // Order by most recent, then skip as many as required. The rest should be returned.
        return _dbContext.Executions
            .AsNoTracking()
            .Where(e => e.JobId == jobId)
            .OrderByDescending(e => e.StartedOn)
            .Skip(skipCount)
            .ToList();
    }

    public void DeleteExecution(ExecutionModel execution)
    {
        // First, delete the applicable filesystem entries
        var filePathname = GetOutputPathName(execution, false);
        File.Delete(filePathname);

        // Next, delete the parent year-month directory if it's empty
        var yearMonthDir = Path.GetDirectoryName(filePathname);
        if (!string.IsNullOrWhiteSpace(yearMonthDir) && Directory.GetFileSystemEntries(yearMonthDir).Length == 0)
        {
            Directory.Delete(yearMonthDir, false);
        }

        // Now, delete the DB entry
        WriteWithRetry(() =>
        {
            _dbContext.Executions
                .Where(e => e.Id == execution.Id)
                .ExecuteDelete();
        });
    }

    public List<ExecutionModel> GetExecutionsOlderThan(DateTimeOffset startedOn)
    {
        return _dbContext.Executions
            .AsNoTracking()
            .Where(e => e.StartedOn < startedOn)
            .ToList();
    }

    private string GetDataLocation()
    {
        var location = _configuration["DataLocation"];
        if (location == null)
        {
            _logger.LogError("Cannot determine location of execution data: DataLocation is not set");
            throw new InvalidOperationException();
        }
        return Path.GetFullPath(location);
    }

    private void WriteWithRetry(Action writeAction, int retryCount = 3)
    {
        Exception? lastException = null;
        for (int i = 0; i < retryCount; i++)
        {
            try
            {
                using var transaction = _dbContext.Database.BeginTransaction();
                writeAction.Invoke();
                transaction.Commit();

                // Success!
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database write attempt #{Attempt} failed, retrying up to {RetryCount} times", i + 1, retryCount);
                lastException = ex;
                Thread.Sleep(20);

                // Retry if count allows for it...
            }
        }

        // Failed!
        throw new Exception("Database write with retry failed!", lastException);
    }
}
