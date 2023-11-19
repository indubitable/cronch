namespace cronch.Models.ViewModels;

public readonly record struct ExecutionViewModel(Guid JobId, Guid ExecutionId, string JobName, DateTimeOffset StartedOn, DateTimeOffset? CompletedOn, ExecutionStatus Status)
{
    public TimeSpan Duration
    {
        get
        {
            return (CompletedOn.HasValue ? CompletedOn.Value.Subtract(StartedOn) : DateTimeOffset.UtcNow.Subtract(StartedOn));
        }
    }
}
