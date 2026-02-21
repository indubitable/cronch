namespace cronch.Models.ViewModels;

public readonly record struct ExecutionViewModel(Guid JobId, Guid ExecutionId, string JobName, DateTimeOffset StartedOn, DateTimeOffset? CompletedOn, ExecutionStatus Status, ExecutionReason? StartReason, TerminationReason? StopReason)
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
            if (duration.TotalSeconds < 1) return "< 1 s";
            if (duration.TotalMinutes < 1) durationFormat = "s\\ \\s";
            //else if (duration.TotalHours < 1) durationFormat = "mm\\:ss";
            else if (duration.TotalDays < 1) durationFormat = "hh\\:mm\\:ss";
            return duration.ToString(durationFormat);
        }
    }

    public string RelativeStartTime
    {
        get
        {
            var age = DateTimeOffset.UtcNow - StartedOn;
            if (age.TotalSeconds < 60) return "just now";
            if (age.TotalMinutes < 60) return $"{(int)age.TotalMinutes} min ago";
            if (age.TotalHours < 6) return $"{(int)age.TotalHours} hr ago";
            return StartedOn.ToLocalTime().ToString("G");
        }
    }
}
