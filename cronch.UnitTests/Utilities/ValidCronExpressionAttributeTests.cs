using cronch.Utilities;
using System.ComponentModel.DataAnnotations;

namespace cronch.UnitTests.Utilities;

[TestClass]
public class ValidCronExpressionAttributeTests
{
    private static ValidationResult? Validate(string? cronSchedule)
    {
        var attribute = new ValidCronExpressionAttribute();
        var context = new ValidationContext(new object()) { MemberName = "CronSchedule" };
        return attribute.GetValidationResult(cronSchedule, context);
    }

    [TestMethod]
    public void NullShouldBeValid()
    {
        Assert.AreEqual(ValidationResult.Success, Validate(null));
    }

    [TestMethod]
    public void EmptyStringShouldBeValid()
    {
        Assert.AreEqual(ValidationResult.Success, Validate(""));
    }

    [TestMethod]
    public void WhitespaceShouldBeValid()
    {
        Assert.AreEqual(ValidationResult.Success, Validate("   "));
    }

    [TestMethod]
    [DataRow("0 0 12 * * ?")]
    [DataRow("0 */5 * * * ?")]
    [DataRow("0 0 0 ? * MON-FRI")]
    public void ValidExpressionShouldPass(string cron)
    {
        Assert.AreEqual(ValidationResult.Success, Validate(cron));
    }

    [TestMethod]
    [DataRow("not a cron")]
    [DataRow("* * *")]
    [DataRow("60 * * * * ?")]
    [DataRow("0 0 25 * * ?")]
    public void InvalidExpressionShouldFail(string cron)
    {
        var result = Validate(cron);
        Assert.AreNotEqual(ValidationResult.Success, result);
        Assert.IsTrue(result!.MemberNames.Contains("CronSchedule"));
    }

    [TestMethod]
    public void ErrorMessageShouldMentionParsing()
    {
        var result = Validate("not a cron");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ErrorMessage!.Contains("could not be parsed", StringComparison.OrdinalIgnoreCase));
    }
}
