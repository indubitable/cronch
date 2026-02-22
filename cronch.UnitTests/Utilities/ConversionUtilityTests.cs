using cronch.Models;
using cronch.Models.ViewModels;
using cronch.Utilities;

namespace cronch.UnitTests.Utilities;

[TestClass]
public class ConversionUtilityTests
{
    // --- ToEnumWithFallback ---

    [TestMethod]
    public void ToEnumWithFallbackShouldReturnCorrespondingEnumForValidValue()
    {
        var result = "CompletedAsSuccess".ToEnumWithFallback(ExecutionStatus.Unknown);

        Assert.AreEqual(ExecutionStatus.CompletedAsSuccess, result);
    }

    [TestMethod]
    public void ToEnumWithFallbackShouldReturnFallbackForNullInput()
    {
        string? value = null;

        var result = value!.ToEnumWithFallback(ExecutionStatus.Unknown);

        Assert.AreEqual(ExecutionStatus.Unknown, result);
    }

    [TestMethod]
    public void ToEnumWithFallbackShouldReturnFallbackForWhitespaceInput()
    {
        var result = "   ".ToEnumWithFallback(ExecutionStatus.Unknown);

        Assert.AreEqual(ExecutionStatus.Unknown, result);
    }

    [TestMethod]
    public void ToEnumWithFallbackShouldReturnFallbackForUnrecognizedValue()
    {
        var result = "NotARealStatus".ToEnumWithFallback(ExecutionStatus.Unknown);

        Assert.AreEqual(ExecutionStatus.Unknown, result);
    }

    // --- Keywords parsing via ToModel(JobViewModel) ---

    [TestMethod]
    public void ToModelFromJobViewModelShouldReturnSingleKeywordItem()
    {
        var vm = MinimalJobViewModel();
        vm.Keywords = "error";

        var model = vm.ToModel();

        Assert.HasCount(1, model.Keywords);
        Assert.AreEqual("error", model.Keywords[0]);
    }

    [TestMethod]
    public void ToModelFromJobViewModelShouldTrimWhitespaceFromEachKeyword()
    {
        var vm = MinimalJobViewModel();
        vm.Keywords = "  error  ,  warning  ,critical";

        var model = vm.ToModel();

        CollectionAssert.AreEqual(new[] { "error", "warning", "critical" }, model.Keywords);
    }

    [TestMethod]
    [Description("Documents current behavior: empty/null keywords produce [\"\"], not []. " +
                 "An empty keyword will match every output line because line.Contains(\"\") is always true, " +
                 "so enabling keyword matching with empty keywords effectively means matching everything.")]
    public void ToModelFromJobViewModelShouldProduceListWithEmptyStringWhenKeywordsIsEmpty()
    {
        var vm = MinimalJobViewModel();
        vm.Keywords = "";

        var model = vm.ToModel();

        Assert.HasCount(1, model.Keywords);
        Assert.AreEqual("", model.Keywords[0]);
    }

    [TestMethod]
    [Description("Same empty-keyword behavior applies when Keywords is null.")]
    public void ToModelFromJobViewModelShouldProduceListWithEmptyStringWhenKeywordsIsNull()
    {
        var vm = MinimalJobViewModel();
        vm.Keywords = null;

        var model = vm.ToModel();

        Assert.HasCount(1, model.Keywords);
        Assert.AreEqual("", model.Keywords[0]);
    }

    // --- Keywords round-trip ---

    [TestMethod]
    public void ToViewModelFromJobModelShouldJoinKeywordsWithCommaAndNoSpaces()
    {
        var model = new JobModel { Keywords = ["alpha", "beta", "gamma"] };

        var vm = model.ToViewModel();

        Assert.AreEqual("alpha,beta,gamma", vm.Keywords);
    }

    // --- Helpers ---

    private static JobViewModel MinimalJobViewModel() => new()
    {
        Name = "Test",
        Executor = "bash",
        Script = "echo hi",
        CronSchedule = "0 * * * * *",
        StdOutProcessing = "None",
        StdErrProcessing = "None",
        MarkParallelSkipAs = "Ignore",
    };
}
