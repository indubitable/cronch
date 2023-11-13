using cronch.Models.Persistence;
using System.Xml.Serialization;

namespace cronch.Services;

public class JobPersistenceService
{
    private readonly ILogger<JobPersistenceService> _logger;
    private readonly IConfiguration _configuration;
    private readonly XmlSerializer _serializer = new(typeof(ExecutionPersistenceModel));

    public JobPersistenceService(ILogger<JobPersistenceService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public virtual DateTimeOffset? GetLatestExecutionForJob(Guid jobId)
    {
        var executionsFile = Path.Combine(GetDataLocation(), jobId.ToString("D"), "executions.csv");
        if (!File.Exists(executionsFile))
        {
            return null;
        }

        try
        {
            using var fileReader = new StreamReader(executionsFile, new FileStreamOptions { Access = FileAccess.Read });
            var latest = string.Empty;
            while (true)
            {
                var line = fileReader.ReadLine();
                if (line == null) break;
                if (line.EndsWith(",False")) continue;
                latest = line;
            }
            if (latest.Length > 0)
            {
                return DateTimeOffset.ParseExact(latest.AsSpan(0, 19), "s", null, System.Globalization.DateTimeStyles.AssumeUniversal);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to read executions for job {Id}", jobId);
        }
        return null;
    }

    public virtual void SaveLatestExecutionForJob(ExecutionPersistenceModel execution, bool considerAsProperExecution)
    {
        try
        {
            File.AppendAllText(Path.Combine(GetDataLocation(), execution.JobId.ToString("D"), "executions.csv"), $"{execution.StartedOn:s},\"{GetJobRelativeExecutionPath(execution)}\",{considerAsProperExecution}\n");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to save latest execution for job {JobId}", execution.JobId);
        }
    }

    public virtual void SaveExecution(ExecutionPersistenceModel execution)
    {
        var filePathname = GetExecutionPathName(execution, "metadata.xml", true);

        try
        {
            using var fileStream = File.Open(filePathname, FileMode.Create, FileAccess.Write, FileShare.Read);
            _serializer.Serialize(fileStream, execution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot save execution: unexpected error");
            throw;
        }
    }

    public virtual string GetExecutionPathName(ExecutionPersistenceModel execution, string relativeFileName, bool createParentDirectory)
    {
        var directory = Path.Combine(GetDataLocation(), execution.JobId.ToString("D"), GetJobRelativeExecutionPath(execution));
        if (createParentDirectory)
        {
            Directory.CreateDirectory(directory);
        }

        return Path.Combine(directory, relativeFileName);
    }

    private static string GetJobRelativeExecutionPath(ExecutionPersistenceModel execution)
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
