using System.Xml.Serialization;

namespace cronch.Models.Persistence;

public class ChainRulePersistenceModel
{
	[XmlElement(Order = 0)]
	public Guid TargetJobId { get; set; }

	[XmlElement(Order = 1)]
	public bool RunOnSuccess { get; set; }

	[XmlElement(Order = 2)]
	public bool RunOnIndeterminate { get; set; }

	[XmlElement(Order = 3)]
	public bool RunOnWarning { get; set; }

	[XmlElement(Order = 4)]
	public bool RunOnError { get; set; }
}
