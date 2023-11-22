using System.Xml.Serialization;

namespace cronch.Models.Persistence;

[Serializable, XmlRoot("Configuration", Namespace = "urn:indubitable-software:cronch:v1", IsNullable = false)]
public class ConfigPersistenceModel
{
    [XmlArray(Order = 0), XmlArrayItem("Job")]
    public List<JobPersistenceModel> Jobs { get; set; } = [];

    [XmlElement(Order = 1, IsNullable = true)]
    public int? MaxHistoryItemsShown { get; set; }

    [XmlElement(Order = 2, IsNullable = true)]
    public int? DeleteHistoricalRunsAfterCount { get; set; }

    [XmlElement(Order = 3, IsNullable = true)]
    public int? DeleteHistoricalRunsAfterDays { get; set; }
}
