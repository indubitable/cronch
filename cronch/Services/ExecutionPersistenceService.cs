using cronch.Models;
using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace cronch.Services;

public partial class ExecutionPersistenceService(ILogger<ExecutionPersistenceService> _logger, IConfiguration _configuration)
{
    public readonly record struct ExecutionStatistics(int Successes, int Errors, int Warnings);

    private static string _connectionString = null!;

    public virtual void InitializeDatabase()
    {
        var dataLocation = _configuration["DataLocation"] ?? throw new ArgumentNullException("DataLocation", "The DataLocation configuration option is missing");
        Directory.CreateDirectory(dataLocation);

        SQLitePCL.Batteries_V2.Init();
        var result = SQLitePCL.raw.sqlite3_config(SQLitePCL.raw.SQLITE_CONFIG_SERIALIZED);
        if (result != SQLitePCL.raw.SQLITE_OK)
        {
            Console.WriteLine($"sqlite3_config for multithreading failed: {result}");
        }
        var dbFile = Path.GetFullPath(Path.Combine(dataLocation, "executions.db"));
        _connectionString = new SqliteConnectionStringBuilder()
        {
            DataSource = dbFile,
            Pooling = true,
            Cache = SqliteCacheMode.Private,
            DefaultTimeout = 10,
        }.ToString();

        using var db = new SqliteConnection(_connectionString);
        db.Open();
        db.Execute(@"PRAGMA journal_mode=WAL");
        db.Execute(@"DROP TABLE IF EXISTS __EFMigrationsHistory");
        db.Execute(@"
            CREATE TABLE IF NOT EXISTS ""Execution"" (
                ""Id"" TEXT NOT NULL CONSTRAINT ""PK_Execution"" PRIMARY KEY,
                ""JobId"" TEXT NOT NULL,
                ""JobName"" TEXT NOT NULL,
                ""StartedOn"" TEXT NOT NULL,
                ""StartReason"" TEXT NOT NULL,
                ""Status"" TEXT NOT NULL,
                ""CompletedOn"" TEXT NULL,
                ""ExitCode"" INTEGER NULL,
                ""StopReason"" TEXT NULL
            )
        ");
    }

    public virtual Dictionary<Guid, DateTimeOffset> GetLatestExecutionsPerJob()
    {
        using var db = new SqliteConnection(_connectionString);
        db.Open();
        using var reader = db.ExecuteReader(@"SELECT t1.JobId, t1.StartedOn FROM Execution t1 JOIN (SELECT JobId, MAX(StartedOn) AS StartedOn FROM Execution GROUP BY JobId) t2 ON t1.JobId=t2.JobId AND t1.StartedOn=t2.StartedOn");
        Dictionary<Guid, DateTimeOffset> result = [];
        while(reader.Read())
        {
            result.Add(Guid.ParseExact(reader.GetString(0), "D"), ParseDate(reader.GetString(1)));
        }
        return result;
    }

    public virtual ExecutionStatistics GetExecutionStatistics(DateTimeOffset from,  DateTimeOffset to)
    {
        using var db = new SqliteConnection(_connectionString);
        db.Open();
        var statuses = db.Query<string>(@"SELECT Status FROM Execution WHERE StartedOn > @from AND StartedOn <= @to", new { from = FormatDate(from), to = FormatDate(to) });

        return new ExecutionStatistics
        {
            Successes = statuses.Count(s => s == ExecutionStatus.CompletedAsSuccess.ToString()),
            Errors = statuses.Count(s => s == ExecutionStatus.CompletedAsError.ToString()),
            Warnings = statuses.Count(s => s == ExecutionStatus.CompletedAsWarning.ToString()),
        };
    }

    public virtual ExecutionModel? GetExecution(Guid id)
    {
        using var db = new SqliteConnection(_connectionString);
        db.Open();
        var rawData = db.QuerySingleOrDefault(@"SELECT * FROM Execution WHERE Id = @id", new { id = id.ToString("D").ToUpperInvariant() });
        return ReadModel(rawData);
    }

    public virtual IEnumerable<ExecutionModel> GetRecentExecutions(int maxCount, Guid? jobId)
    {
        using var db = new SqliteConnection(_connectionString);
        db.Open();
        IEnumerable<dynamic> rawData;
        if (jobId.HasValue)
        {
            rawData = db.Query(@"SELECT * FROM Execution WHERE JobId = @jobId ORDER BY StartedOn DESC LIMIT @limit", new { jobId = jobId.Value.ToString("D").ToUpperInvariant(), limit = maxCount });
        }
        else
        {
            rawData = db.Query(@"SELECT * FROM Execution ORDER BY StartedOn DESC LIMIT @limit", new { limit = maxCount });
        }

        foreach (var rawItem in rawData)
        {
            var model = ReadModel(rawItem);
            if (model != null)
            {
                yield return model;
            }
        }
    }

    public virtual void AddExecution(ExecutionModel execution)
    {
        try
        {
            using var db = new SqliteConnection(_connectionString);
            db.Open();
            db.Execute(@"INSERT INTO Execution (Id, JobId, JobName, StartedOn, StartReason, Status, CompletedOn, ExitCode, StopReason) VALUES (@id, @jobId, @jobName, @startedOn, @startReason, @status, @completedOn, @exitCode, @stopReason)",
                new
                {
                    id = execution.Id.ToString("D").ToUpperInvariant(),
                    jobId = execution.JobId.ToString("D").ToUpperInvariant(),
                    jobName = execution.JobName,
                    startedOn = FormatDate(execution.StartedOn),
                    startReason = execution.StartReason.ToString(),
                    status = execution.Status.ToString(),
                    completedOn = execution.CompletedOn.HasValue ? FormatDate(execution.CompletedOn.Value) : null,
                    exitCode = execution.ExitCode,
                    stopReason = execution.StopReason.ToString(),
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
            using var db = new SqliteConnection(_connectionString);
            db.Open();
            db.Execute(@"UPDATE Execution SET JobId = @jobId, JobName = @jobName, StartedOn = @startedOn, StartReason = @startReason, Status = @status, CompletedOn = @completedOn, ExitCode = @exitCode, StopReason = @stopReason WHERE Id = @id",
                new
                {
                    id = execution.Id.ToString("D").ToUpperInvariant(),
                    jobId = execution.JobId.ToString("D").ToUpperInvariant(),
                    jobName = execution.JobName,
                    startedOn = FormatDate(execution.StartedOn),
                    startReason = execution.StartReason.ToString(),
                    status = execution.Status.ToString(),
                    completedOn = execution.CompletedOn.HasValue ? FormatDate(execution.CompletedOn.Value) : null,
                    exitCode = execution.ExitCode,
                    stopReason = execution.StopReason.ToString(),
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

    public virtual void RemoveAllDatabaseRecordsForJobId(Guid id)
    {
        using var db = new SqliteConnection(_connectionString);
        db.Open();
        db.Execute(@"DELETE FROM Execution WHERE JobId = @jobId", new { jobId = id.ToString("D").ToUpperInvariant() });
    }

    public virtual IEnumerable<ExecutionModel> GetOldestExecutionsAfterCount(Guid jobId, int skipCount)
    {
        // Order by most recent, then skip as many as required. The rest should be returned.
        using var db = new SqliteConnection(_connectionString);
        db.Open();
        var rawData = db.Query(@"SELECT * FROM Execution WHERE JobId = @jobId ORDER BY StartedOn DESC LIMIT -1 OFFSET @offset", new { jobId = jobId.ToString("D").ToUpperInvariant(), offset = skipCount });

        foreach (var rawItem in rawData)
        {
            var model = ReadModel(rawItem);
            if (model != null)
            {
                yield return model;
            }
        }
    }

    public virtual void DeleteExecution(ExecutionModel execution)
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
        using var db = new SqliteConnection(_connectionString);
        db.Open();
        db.Execute(@"DELETE FROM Execution WHERE Id = @id", new { id = execution.Id.ToString("D").ToUpperInvariant() });
    }

    public virtual IEnumerable<ExecutionModel> GetExecutionsOlderThan(DateTimeOffset startedOn)
    {
        using var db = new SqliteConnection(_connectionString);
        db.Open();
        var rawData = db.Query(@"SELECT * FROM Execution WHERE StartedOn < @startedOn", new { startedOn = FormatDate(startedOn) });

        foreach (var rawItem in rawData)
        {
            var model = ReadModel(rawItem);
            if (model != null)
            {
                yield return model;
            }
        }
    }

    public virtual void CleanUpExecutionFilesForDeletedJobs(IEnumerable<Guid> knownJobIds)
    {
        var allDataSubdirectories = Directory.EnumerateDirectories(GetDataLocation())
            .Select(d => d.EndsWith(Path.DirectorySeparatorChar) ? d[0..^1] : d)
            .Where(d => Guid.TryParseExact(Path.GetFileName(d), "D", out var _));
        foreach (var subdir in allDataSubdirectories)
        {
            if (!Guid.TryParseExact(Path.GetFileName(subdir), "D", out var id)) continue;

            if (!knownJobIds.Contains(id))
            {
                // Found one! Remove it.
                try
                {
                    RemoveAllDatabaseRecordsForJobId(id);
                    Directory.Delete(subdir, true);
                    _logger.LogInformation("Deleted data for nonexistent job {Id}", id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to delete data for nonexistent job {Id}", id);
                }
            }
        }
    }

    public virtual void CleanUpOrphanedExecutionFiles(Guid jobId)
    {
        var knownIds = GetAllExecutionIdsForJob(jobId);
        var regex = GetExecutionFileRegex();
        var parentDir = Path.Combine(GetDataLocation(), jobId.ToString("D"));
        if (Directory.Exists(parentDir))
        {
            foreach (var dateDir in Directory.EnumerateDirectories(parentDir))
            {
                foreach (var execFile in Directory.EnumerateFiles(dateDir))
                {
                    var match = regex.Match(execFile);
                    if (match.Success && !knownIds.Contains(Guid.ParseExact(match.Groups["guid"].Value, "N")))
                    {
                        // This is an orphan. Remove it.
                        File.Delete(execFile);
                    }
                }
                if (Directory.GetFileSystemEntries(dateDir).Length == 0)
                {
                    // Delete the empty date directory.
                    Directory.Delete(dateDir, false);
                }
            }
            if (Directory.GetFileSystemEntries(parentDir).Length == 0)
            {
                // Delete the empty parent directory.
                Directory.Delete(parentDir, false);
            }
        }
    }

    [GeneratedRegex(@".*(?<guid>[0-9a-f]{32})\.txt")]
    private static partial Regex GetExecutionFileRegex();

    private List<Guid> GetAllExecutionIdsForJob(Guid jobId)
    {
        using var db = new SqliteConnection(_connectionString);
        db.Open();
        return db.Query<string>(@"SELECT Id FROM Execution WHERE JobId = @jobId", new { jobId })
            .Select(g => Guid.ParseExact(g, "D"))
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

    private static DateTimeOffset ParseDate(string dateString)
    {
        return DateTimeOffset.Parse(dateString, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AssumeUniversal);
    }

    private static string FormatDate(DateTimeOffset date)
    {
        return date.ToUniversalTime().ToString("yyyy'-'MM'-'dd HH':'mm':'ss'.'fffffff", CultureInfo.InvariantCulture.DateTimeFormat);
    }

    private static ExecutionModel? ReadModel(dynamic? modelData)
    {
        if (modelData == null) return null;

        return new ExecutionModel
        {
            Id = Guid.ParseExact(modelData.Id, "D"),
            JobId = Guid.ParseExact(modelData.JobId, "D"),
            JobName = modelData.JobName,
            StartedOn = ParseDate(modelData.StartedOn),
            StartReason = Enum.Parse<ExecutionReason>(modelData.StartReason),
            Status = Enum.Parse<ExecutionStatus>(modelData.Status),
            CompletedOn = (string.IsNullOrWhiteSpace(modelData.CompletedOn) ? null : ParseDate(modelData.CompletedOn)),
            ExitCode = modelData.ExitCode as int?,
            StopReason = (string.IsNullOrWhiteSpace(modelData.StopReason) ? null : Enum.Parse<TerminationReason>(modelData.StopReason)),
        };
    }
}
