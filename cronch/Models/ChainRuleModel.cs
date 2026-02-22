namespace cronch.Models;

public class ChainRuleModel
{
	public Guid TargetJobId { get; set; }
	public bool RunOnSuccess { get; set; }
	public bool RunOnIndeterminate { get; set; }
	public bool RunOnWarning { get; set; }
	public bool RunOnError { get; set; }
}
