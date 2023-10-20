using System.Xml.Serialization;

namespace cronch.Models.Persistence;

[Serializable, XmlRoot("Configuration", Namespace = "urn:indubitable-software:cronch:v1", IsNullable = false)]
public class ConfigPersistenceModel
{
    [XmlArray(Order = 0), XmlArrayItem("Job")]
    public List<JobPersistenceModel> Jobs { get; set; } = new();
}
