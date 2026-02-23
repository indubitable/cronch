using cronch.Utilities;

namespace cronch.UnitTests.Utilities;

[TestClass]
public class CronExpressionConverterTests
{
    // --- Null / empty / whitespace passthrough ---

    [TestMethod]
    public void TryConvertShouldReturnNullForNull()
    {
        Assert.IsNull(CronExpressionConverter.TryConvertCronosToQuartz(null));
    }

    [TestMethod]
    public void TryConvertShouldReturnEmptyForEmpty()
    {
        Assert.AreEqual("", CronExpressionConverter.TryConvertCronosToQuartz(""));
    }

    [TestMethod]
    public void TryConvertShouldReturnWhitespaceForWhitespace()
    {
        Assert.AreEqual("   ", CronExpressionConverter.TryConvertCronosToQuartz("   "));
    }

    // --- Basic wildcard conversions ---

    [TestMethod]
    public void ShouldConvertAllWildcardsToQuartzFormat()
    {
        // Cronos: * * * * * *  →  Quartz: * * * * * ? (dow becomes ?)
        var result = CronExpressionConverter.ConvertCronosToQuartz("* * * * * *");
        Assert.AreEqual("* * * * * ?", result);
    }

    // --- Day-of-week numeric conversion ---

    [TestMethod]
    public void ShouldIncrementSingleDowValue()
    {
        // Cronos: 0 0 12 ? * 1  (Monday) → Quartz: 0 0 12 ? * 2
        var result = CronExpressionConverter.ConvertCronosToQuartz("0 0 12 * * 1");
        Assert.AreEqual("0 0 12 ? * 2", result);
    }

    [TestMethod]
    public void ShouldConvertDow0SundayToQuartz1()
    {
        // Cronos: 0=SUN → Quartz: 1=SUN
        var result = CronExpressionConverter.ConvertCronosToQuartz("0 0 12 * * 0");
        Assert.AreEqual("0 0 12 ? * 1", result);
    }

    [TestMethod]
    public void ShouldConvertDow7SundayToQuartz1()
    {
        // Cronos: 7=SUN (alias) → Quartz: 1=SUN
        var result = CronExpressionConverter.ConvertCronosToQuartz("0 0 12 * * 7");
        Assert.AreEqual("0 0 12 ? * 1", result);
    }

    [TestMethod]
    public void ShouldConvertDow6SaturdayToQuartz7()
    {
        // Cronos: 6=SAT → Quartz: 7=SAT
        var result = CronExpressionConverter.ConvertCronosToQuartz("0 0 12 * * 6");
        Assert.AreEqual("0 0 12 ? * 7", result);
    }

    // --- Day-of-week ranges ---

    [TestMethod]
    public void ShouldConvertDowRange()
    {
        // Cronos: 1-5 (Mon-Fri) → Quartz: 2-6
        var result = CronExpressionConverter.ConvertCronosToQuartz("0 0 9 * * 1-5");
        Assert.AreEqual("0 0 9 ? * 2-6", result);
    }

    // --- Day-of-week lists ---

    [TestMethod]
    public void ShouldConvertDowList()
    {
        // Cronos: 0,3,6 (Sun,Wed,Sat) → Quartz: 1,4,7
        var result = CronExpressionConverter.ConvertCronosToQuartz("0 0 9 * * 0,3,6");
        Assert.AreEqual("0 0 9 ? * 1,4,7", result);
    }

    // --- Day-of-week steps ---

    [TestMethod]
    public void ShouldConvertDowStep()
    {
        // Cronos: 1/2 → Quartz: 2/2
        var result = CronExpressionConverter.ConvertCronosToQuartz("0 0 9 * * 1/2");
        Assert.AreEqual("0 0 9 ? * 2/2", result);
    }

    // --- Named days (no conversion needed) ---

    [TestMethod]
    public void ShouldPreserveNamedDays()
    {
        var result = CronExpressionConverter.ConvertCronosToQuartz("0 0 9 * * MON-FRI");
        Assert.AreEqual("0 0 9 ? * MON-FRI", result);
    }

    // --- Question mark rule ---

    [TestMethod]
    public void ShouldSetDomToQuestionMarkWhenDowIsSpecific()
    {
        // dom=* and dow=specific → dom becomes ?
        var result = CronExpressionConverter.ConvertCronosToQuartz("0 0 12 * * 1");
        Assert.AreEqual("0 0 12 ? * 2", result);
    }

    [TestMethod]
    public void ShouldSetDowToQuestionMarkWhenDomIsSpecific()
    {
        // dom=15 and dow=* → dow becomes ?
        var result = CronExpressionConverter.ConvertCronosToQuartz("0 0 12 15 * *");
        Assert.AreEqual("0 0 12 15 * ?", result);
    }

    [TestMethod]
    public void ShouldPreserveDomWhenDowIsAlreadyWild()
    {
        var result = CronExpressionConverter.ConvertCronosToQuartz("0 30 6 1-15 * *");
        Assert.AreEqual("0 30 6 1-15 * ?", result);
    }

    // --- Every-second expression ---

    [TestMethod]
    public void ShouldConvertEverySecondExpression()
    {
        var result = CronExpressionConverter.ConvertCronosToQuartz("* * * * * *");
        Assert.AreEqual("* * * * * ?", result);
    }

    // --- Every-minute expression ---

    [TestMethod]
    public void ShouldConvertEveryMinuteExpression()
    {
        var result = CronExpressionConverter.ConvertCronosToQuartz("0 * * * * *");
        Assert.AreEqual("0 * * * * ?", result);
    }

    // --- Error cases ---

    [TestMethod]
    public void ShouldThrowForWrongNumberOfFields()
    {
        Assert.ThrowsExactly<FormatException>(() =>
            CronExpressionConverter.ConvertCronosToQuartz("* * * * *"));
    }

    [TestMethod]
    public void TryConvertShouldReturnNullForInvalidExpression()
    {
        var result = CronExpressionConverter.TryConvertCronosToQuartz("not a cron");
        Assert.IsNull(result);
    }
}
