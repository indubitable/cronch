using CronExpressionDescriptor;
using Quartz;

namespace cronch.Utilities;

/// <summary>
/// Centralizes CronExpressionDescriptor usage to ensure consistent configuration
/// for Quartz.NET cron expressions (1-based day-of-week, '?' wildcard).
/// </summary>
public static class CronDescriptionUtility
{
    internal static readonly Options DescriptorOptions = new()
    {
        DayOfWeekStartIndexZero = false,
        Use24HourTimeFormat = false,
    };

    /// <summary>
    /// Returns a human-readable description of a Quartz.NET cron expression.
    /// Throws <see cref="FormatException"/> if the expression is invalid.
    /// Returns an empty string if the expression is null or whitespace.
    /// </summary>
    /// <exception cref="FormatException">Thrown when the cron expression is not valid.</exception>
    public static string Describe(string? cronSchedule)
    {
        if (string.IsNullOrWhiteSpace(cronSchedule))
            return string.Empty;

        try
        {
            _ = new CronExpression(cronSchedule);
        }
        catch (Exception ex)
        {
            throw new FormatException($"Invalid cron expression: {cronSchedule}", ex);
        }

        return ExpressionDescriptor.GetDescription(cronSchedule, DescriptorOptions);
    }
}
