using cronch.Models.Persistence;
using System.Xml.Serialization;

namespace cronch.Services;

public class ConfigPersistenceService(ILogger<ConfigPersistenceService> _logger, IConfiguration _configuration)
{
    private readonly XmlSerializer _serializer = new(typeof(ConfigPersistenceModel));
    private const string CONFIG_FILE = "config.xml";

    public ConfigPersistenceModel? Load()
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

        try
        {
            using var fileStream = File.OpenRead(filePathname);
            var config = _serializer.Deserialize(fileStream) as ConfigPersistenceModel;
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot load configuration: unexpected error");
            throw;
        }
    }

    public void Save(ConfigPersistenceModel model)
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

    private static void ArrangeModel(ConfigPersistenceModel model)
    {
        model.Jobs.Sort((a, b) => a.Id.CompareTo(b.Id));
    }
}
