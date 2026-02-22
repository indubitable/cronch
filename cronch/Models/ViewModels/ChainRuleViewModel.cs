namespace cronch.Models.ViewModels;

public class ChainRuleViewModel
{
	public Guid TargetJobId { get; set; }
	public bool RunOnSuccess { get; set; }
	public bool RunOnIndeterminate { get; set; }
	public bool RunOnWarning { get; set; }
	public bool RunOnError { get; set; }
}
