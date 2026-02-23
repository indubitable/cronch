using cronch.Models.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace cronch.UnitTests.ViewModels;

[TestClass]
public class JobViewModelTests
{
    private static List<ValidationResult> RunValidation(JobViewModel vm)
    {
        var context = new ValidationContext(vm);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(vm, context, results, validateAllProperties: true);
        return results;
    }

    private static JobViewModel CreateMinimalValidVm(string? cronSchedule = "0 0 12 * * ?") => new()
    {
        Name = "Test Job",
        Enabled = true,
        CronSchedule = cronSchedule,
        Executor = "/bin/bash",
        Script = "echo hello",
        MarkParallelSkipAs = "Skipped",
        StdOutProcessing = "None",
        StdErrProcessing = "None",
    };

    [TestMethod]
    public void ValidCronScheduleShouldPassValidation()
    {
        var vm = CreateMinimalValidVm("0 0 12 * * ?");

        var results = RunValidation(vm);

        Assert.IsFalse(results.Any(r => r.MemberNames.Contains(nameof(JobViewModel.CronSchedule))),
            "Expected no CronSchedule validation errors for a valid expression.");
    }

    [TestMethod]
    public void NullCronScheduleShouldPassValidation()
    {
        var vm = CreateMinimalValidVm(cronSchedule: null);

        var results = RunValidation(vm);

        Assert.IsFalse(results.Any(r => r.MemberNames.Contains(nameof(JobViewModel.CronSchedule))),
            "Expected no CronSchedule errors when schedule is null (schedule-less job).");
    }

    [TestMethod]
    public void EmptyCronScheduleShouldPassValidation()
    {
        var vm = CreateMinimalValidVm(cronSchedule: "");

        var results = RunValidation(vm);

        Assert.IsFalse(results.Any(r => r.MemberNames.Contains(nameof(JobViewModel.CronSchedule))),
            "Expected no CronSchedule errors when schedule is empty (schedule-less job).");
    }

    [TestMethod]
    public void WhitespaceCronScheduleShouldPassValidation()
    {
        var vm = CreateMinimalValidVm(cronSchedule: "   ");

        var results = RunValidation(vm);

        Assert.IsFalse(results.Any(r => r.MemberNames.Contains(nameof(JobViewModel.CronSchedule))),
            "Expected no CronSchedule errors when schedule is whitespace.");
    }

    [TestMethod]
    [DataRow("not a cron")]
    [DataRow("* * *")]
    [DataRow("60 * * * * ?")]
    [DataRow("0 0 25 * * ?")]
    [DataRow("0 0 0 32 * ?")]
    public void InvalidCronScheduleShouldFailValidation(string invalidCron)
    {
        var vm = CreateMinimalValidVm(cronSchedule: invalidCron);

        var results = RunValidation(vm);

        Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(JobViewModel.CronSchedule))),
            $"Expected a CronSchedule validation error for invalid expression '{invalidCron}'.");
    }

    [TestMethod]
    [DataRow("0 0 12 * * ?")]
    [DataRow("0 */5 * * * ?")]
    [DataRow("0 0 0 ? * MON-FRI")]
    [DataRow("0 30 6 * * ?")]
    public void ValidCronExpressionsShouldPassValidation(string validCron)
    {
        var vm = CreateMinimalValidVm(cronSchedule: validCron);

        var results = RunValidation(vm);

        Assert.IsFalse(results.Any(r => r.MemberNames.Contains(nameof(JobViewModel.CronSchedule))),
            $"Expected no CronSchedule validation error for valid expression '{validCron}'.");
    }

    [TestMethod]
    public void InvalidCronValidationErrorShouldMentionParsing()
    {
        var vm = CreateMinimalValidVm(cronSchedule: "not a cron");

        var results = RunValidation(vm);

        var cronError = results.FirstOrDefault(r => r.MemberNames.Contains(nameof(JobViewModel.CronSchedule)));
        Assert.IsNotNull(cronError, "Expected a CronSchedule validation error.");
        Assert.IsTrue(cronError.ErrorMessage!.Contains("could not be parsed", StringComparison.OrdinalIgnoreCase),
            $"Error message should mention parsing failure. Got: {cronError.ErrorMessage}");
    }
}
