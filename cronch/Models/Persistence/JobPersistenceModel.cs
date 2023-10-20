using System.Xml.Serialization;

namespace cronch.Models.Persistence;

public class JobPersistenceModel
{
    [XmlElement(Order = 0)]
    public Guid Id { get; set; }

    [XmlElement(Order = 1)]
    public string Name { get; set; } = string.Empty;

    [XmlElement(Order = 2)]
    public bool Enabled { get; set; }

    [XmlElement(Order = 3)]
    public string CronSchedule { get; set; } = string.Empty;

    [XmlElement(Order = 4)]
    public string Executor { get; set; } = string.Empty;

    [XmlElement(Order = 5)]
    public string Script { get; set; } = string.Empty;

    [XmlElement(Order = 6, IsNullable = true)]
    public string? ScriptFilePathname { get; set; }

    [XmlArray(Order = 7), XmlArrayItem("Keyword")]
    public List<string> Keywords { get; set; } = new List<string>();

    [XmlElement(Order = 8)]
    public string StdOutProcessing { get; set; } = string.Empty;

    [XmlElement(Order = 9)]
    public string StdErrProcessing { get; set; } = string.Empty;
}
