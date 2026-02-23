using cronch.Utilities;

namespace cronch.UnitTests.Utilities;

[TestClass]
public class CronDescriptionUtilityTests
{
    [TestMethod]
    public void DescriptorOptionsShouldHaveDayOfWeekStartIndexZeroDisabled()
    {
        // Quartz uses 1-based DOW (1=SUN..7=SAT), so DayOfWeekStartIndexZero must be false
        Assert.IsFalse(CronDescriptionUtility.DescriptorOptions.DayOfWeekStartIndexZero);
    }

    [TestMethod]
    public void DescriptorOptionsShouldUse12HourTimeFormat()
    {
        Assert.IsFalse(CronDescriptionUtility.DescriptorOptions.Use24HourTimeFormat);
    }

    [TestMethod]
    public void DescribeShouldReturnEmptyForNull()
    {
        Assert.AreEqual(string.Empty, CronDescriptionUtility.Describe(null));
    }

    [TestMethod]
    public void DescribeShouldReturnEmptyForWhitespace()
    {
        Assert.AreEqual(string.Empty, CronDescriptionUtility.Describe("   "));
    }

    [TestMethod]
    public void DescribeShouldThrowForInvalidExpression()
    {
        Assert.ThrowsExactly<FormatException>(() => CronDescriptionUtility.Describe("not a cron"));
    }

    [TestMethod]
    public void DescribeShouldReturnNonEmptyForValidQuartzExpression()
    {
        var result = CronDescriptionUtility.Describe("0 0 12 * * ?");

        Assert.AreNotEqual(string.Empty, result);
    }

    [TestMethod]
    public void DescribeShouldThrowForInvalidQuartzExpression()
    {
        // "invalid cron here" should throw FormatException from Quartz validation
        Assert.ThrowsExactly<FormatException>(() => CronDescriptionUtility.Describe("invalid cron here"));
    }
}
