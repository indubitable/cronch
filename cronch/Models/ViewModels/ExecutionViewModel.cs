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

    public string FormattedDuration
    {
        get
        {
            var duration = Duration;
            var durationFormat = "d\\ \\d\\ hh\\:mm\\:ss";
            if (duration.TotalMinutes < 1) durationFormat = "s\\ \\s";
            //else if (duration.TotalHours < 1) durationFormat = "mm\\:ss";
            else if (duration.TotalDays < 1) durationFormat = "hh\\:mm\\:ss";
            return duration.ToString(durationFormat);
        }
    }
}
