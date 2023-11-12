using System.Xml.Serialization;

namespace cronch.Models.Persistence;

public class ExecutionPersistenceModel
{
    [XmlElement]
    public Guid JobId { get; set; }

    [XmlElement]
    public DateTimeOffset StartedOn { get; set; }

    [XmlElement]
    public string Reason { get; set; } = string.Empty;

    [XmlElement]
    public string RandomComponent { get; set; } = string.Empty;

    [XmlElement(IsNullable = true)]
    public DateTimeOffset? CompletedOn { get; set; }

    [XmlElement(IsNullable = true)]
    public int? ExitCode { get; set; }

    public static ExecutionPersistenceModel CreateNew(Guid jobId, string reason)
    {
        return new ExecutionPersistenceModel
        {
            JobId = jobId,
            StartedOn = DateTimeOffset.UtcNow,
            Reason = reason,
            RandomComponent = Path.GetRandomFileName(),
        };
    }

    public string GetExecutionName()
    {
        return $"{StartedOn:yyyy-MM-dd_HH-mm-ss.fff}_{RandomComponent}";
    }

    private ExecutionPersistenceModel() { }
}
