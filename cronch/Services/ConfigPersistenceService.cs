using cronch.Models.Persistence;
using System.Xml.Serialization;

namespace cronch.Services;

public class ConfigPersistenceService(ILogger<ConfigPersistenceService> _logger, IConfiguration _configuration)
{
    private readonly XmlSerializer _serializer = new(typeof(ConfigPersistenceModel));
    private readonly XmlSerializer _v1Serializer = new(typeof(ConfigPersistenceModelV1));
    private const string CONFIG_FILE = "config.xml";

    public virtual ConfigPersistenceModel? Load()
    {
        lock (_serializer)
        {
            var location = _configuration["ConfigLocation"];
            if (location == null)
            {
                _logger.LogError("Cannot load configuration: ConfigLocation is not set");
                throw new InvalidOperationException();
            }

            var filePathname = Path.Combine(location, CONFIG_FILE);

            if (!File.Exists(filePathname))
            {
                _logger.LogDebug("Cannot load configuration: no such file exists");
                return null;
            }

            // Try loading as v2 first
            try
            {
                using var fileStream = File.OpenRead(filePathname);
                if (_serializer.Deserialize(fileStream) is ConfigPersistenceModel config)
                {
                    return config;
                }
            }
            catch (InvalidOperationException)
            {
                // v2 deserialization failed — may be a v1 config, fall through
            }

            // Try loading as v1 and migrate
            try
            {
                _logger.LogInformation("Attempting to load configuration as v1 format for migration");
                ConfigPersistenceModelV1? v1Config;
                using (var fileStream = File.OpenRead(filePathname))
                {
                    v1Config = _v1Serializer.Deserialize(fileStream) as ConfigPersistenceModelV1;
                }
                if (v1Config != null)
                {
                    _logger.LogInformation("Successfully loaded v1 configuration. Migrating to v2...");
                    var v2Config = v1Config.ToV2();

                    // Check for any jobs that failed cron conversion (they were disabled)
                    var failedJobs = v1Config.Jobs
                        .Where(j => j.Enabled && !string.IsNullOrWhiteSpace(j.CronSchedule))
                        .Where(j => v2Config.Jobs.Any(v2j => v2j.Id == j.Id && !v2j.Enabled))
                        .ToList();
                    foreach (var job in failedJobs)
                    {
                        _logger.LogWarning("Job '{Name}' ({Id}) had its cron expression '{Cron}' fail to convert and has been disabled. Please update it manually.",
                            job.Name, job.Id, job.CronSchedule);
                    }

                    Save(v2Config);
                    _logger.LogInformation("Configuration migrated to v2 successfully");
                    return v2Config;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot load configuration: file exists but could not be parsed as v1 or v2");
                throw;
            }

            _logger.LogError("Cannot load configuration: file exists but could not be parsed");
            throw new InvalidOperationException("Configuration file exists but could not be parsed as any known version");
        }
    }

    public virtual void Save(ConfigPersistenceModel model)
    {
        lock (_serializer)
        {
            var location = _configuration["ConfigLocation"];
            if (location == null)
            {
                _logger.LogError("Cannot save configuration: ConfigLocation is not set");
                throw new InvalidOperationException();
            }

            ArrangeModel(model);

            Directory.CreateDirectory(location);
            var filePathname = Path.Combine(location, CONFIG_FILE);

            try
            {
                using var fileStream = File.Open(filePathname, FileMode.Create, FileAccess.Write, FileShare.Read);
                _serializer.Serialize(fileStream, model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot save configuration: unexpected error");
                throw;
            }
        }
    }

    private static void ArrangeModel(ConfigPersistenceModel model) => model.Jobs.Sort((a, b) => a.Id.CompareTo(b.Id));
}
