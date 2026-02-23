using Quartz;
using System.ComponentModel.DataAnnotations;

namespace cronch.Utilities;

/// <summary>
/// Validates that a cron schedule string is a valid Quartz.NET cron expression.
/// Empty/null values are allowed (schedule-less jobs).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class ValidCronExpressionAttribute : ValidationAttribute
{
    public ValidCronExpressionAttribute()
        : base("The provided Cron schedule could not be parsed: {0}")
    {
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var cronSchedule = value as string;
        if (string.IsNullOrWhiteSpace(cronSchedule))
            return ValidationResult.Success;

        try
        {
            var expr = new CronExpression(cronSchedule);
            _ = expr.GetNextValidTimeAfter(DateTimeOffset.UtcNow);
            return ValidationResult.Success;
        }
        catch (Exception ex)
        {
            return new ValidationResult(
                $"The provided Cron schedule could not be parsed: {ex.Message}",
                [validationContext.MemberName!]);
        }
    }
}
