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
    public void ToModelFromJobViewModelShouldProduceEmptyListWhenKeywordsIsEmpty()
    {
        var vm = MinimalJobViewModel();
        vm.Keywords = "";

        var model = vm.ToModel();

        Assert.IsEmpty(model.Keywords);
    }

    [TestMethod]
    public void ToModelFromJobViewModelShouldProduceEmptyListWhenKeywordsIsNull()
    {
        var vm = MinimalJobViewModel();
        vm.Keywords = null;

        var model = vm.ToModel();

        Assert.IsEmpty(model.Keywords);
    }

    // --- Keywords round-trip ---

    [TestMethod]
    public void ToViewModelFromJobModelShouldJoinKeywordsWithCommaAndNoSpaces()
    {
        var model = new JobModel { Keywords = ["alpha", "beta", "gamma"] };

        var vm = model.ToViewModel();

        Assert.AreEqual("alpha,beta,gamma", vm.Keywords);
    }

    // --- ChainRule conversions ---

    [TestMethod]
    public void ToModelFromJobViewModelShouldPreserveChainRules()
    {
        var vm = MinimalJobViewModel();
        var targetId = Guid.NewGuid();
        vm.ChainRules = [new ChainRuleViewModel { TargetJobId = targetId, RunOnSuccess = true, RunOnError = true }];

        var model = vm.ToModel();

        Assert.HasCount(1, model.ChainRules);
        Assert.AreEqual(targetId, model.ChainRules[0].TargetJobId);
        Assert.IsTrue(model.ChainRules[0].RunOnSuccess);
        Assert.IsFalse(model.ChainRules[0].RunOnIndeterminate);
        Assert.IsFalse(model.ChainRules[0].RunOnWarning);
        Assert.IsTrue(model.ChainRules[0].RunOnError);
    }

    [TestMethod]
    public void ToViewModelFromJobModelShouldPreserveChainRules()
    {
        var targetId = Guid.NewGuid();
        var model = new JobModel
        {
            ChainRules = [new ChainRuleModel { TargetJobId = targetId, RunOnWarning = true, RunOnIndeterminate = true }],
        };

        var vm = model.ToViewModel();

        Assert.HasCount(1, vm.ChainRules);
        Assert.AreEqual(targetId, vm.ChainRules[0].TargetJobId);
        Assert.IsFalse(vm.ChainRules[0].RunOnSuccess);
        Assert.IsTrue(vm.ChainRules[0].RunOnIndeterminate);
        Assert.IsTrue(vm.ChainRules[0].RunOnWarning);
        Assert.IsFalse(vm.ChainRules[0].RunOnError);
    }

    [TestMethod]
    public void ToPersistenceFromJobModelShouldPreserveChainRules()
    {
        var targetId = Guid.NewGuid();
        var model = new JobModel
        {
            ChainRules = [new ChainRuleModel { TargetJobId = targetId, RunOnSuccess = true }],
        };

        var persistence = model.ToPersistence();

        Assert.HasCount(1, persistence.ChainRules);
        Assert.AreEqual(targetId, persistence.ChainRules[0].TargetJobId);
        Assert.IsTrue(persistence.ChainRules[0].RunOnSuccess);
        Assert.IsFalse(persistence.ChainRules[0].RunOnError);
    }

    [TestMethod]
    public void ToModelFromJobPersistenceModelShouldPreserveChainRules()
    {
        var targetId = Guid.NewGuid();
        var persistence = new cronch.Models.Persistence.JobPersistenceModel
        {
            Name = "Test",
            Executor = "bash",
            Script = "echo",
            CronSchedule = "0 * * * * *",
            MarkParallelSkipAs = "Ignore",
            StdOutProcessing = "None",
            StdErrProcessing = "None",
            ChainRules = [new cronch.Models.Persistence.ChainRulePersistenceModel { TargetJobId = targetId, RunOnError = true }],
        };

        var model = persistence.ToModel();

        Assert.HasCount(1, model.ChainRules);
        Assert.AreEqual(targetId, model.ChainRules[0].TargetJobId);
        Assert.IsFalse(model.ChainRules[0].RunOnSuccess);
        Assert.IsTrue(model.ChainRules[0].RunOnError);
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
