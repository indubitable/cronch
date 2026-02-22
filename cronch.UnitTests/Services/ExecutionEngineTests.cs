using cronch.Models;
using cronch.Services;

namespace cronch.UnitTests.Services;

[TestClass]
public class ExecutionEngineTests
{
    // --- ProcessLineForStatusUpdate: OutputProcessing.None ---

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldNotChangeStatusWhenDirectiveIsNone()
    {
        var status = ExecutionStatus.CompletedAsSuccess;

        ExecutionEngine.ProcessLineForStatusUpdate("some output", [], JobModel.OutputProcessing.None, ref status);

        Assert.AreEqual(ExecutionStatus.CompletedAsSuccess, status);
    }

    // --- ProcessLineForStatusUpdate: OutputProcessing.WarningOnAnyOutput ---

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldSetWarningWhenDirectiveIsWarningOnAnyOutput()
    {
        var status = ExecutionStatus.CompletedAsSuccess;

        ExecutionEngine.ProcessLineForStatusUpdate("some output", [], JobModel.OutputProcessing.WarningOnAnyOutput, ref status);

        Assert.AreEqual(ExecutionStatus.CompletedAsWarning, status);
    }

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldNotDowngradeFromErrorWhenDirectiveIsWarningOnAnyOutput()
    {
        var status = ExecutionStatus.CompletedAsError;

        ExecutionEngine.ProcessLineForStatusUpdate("some output", [], JobModel.OutputProcessing.WarningOnAnyOutput, ref status);

        Assert.AreEqual(ExecutionStatus.CompletedAsError, status);
    }

    // --- ProcessLineForStatusUpdate: OutputProcessing.ErrorOnAnyOutput ---

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldSetErrorWhenDirectiveIsErrorOnAnyOutput()
    {
        var status = ExecutionStatus.CompletedAsSuccess;

        ExecutionEngine.ProcessLineForStatusUpdate("some output", [], JobModel.OutputProcessing.ErrorOnAnyOutput, ref status);

        Assert.AreEqual(ExecutionStatus.CompletedAsError, status);
    }

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldUpgradeFromWarningToErrorWhenDirectiveIsErrorOnAnyOutput()
    {
        var status = ExecutionStatus.CompletedAsWarning;

        ExecutionEngine.ProcessLineForStatusUpdate("some output", [], JobModel.OutputProcessing.ErrorOnAnyOutput, ref status);

        Assert.AreEqual(ExecutionStatus.CompletedAsError, status);
    }

    // --- ProcessLineForStatusUpdate: OutputProcessing.WarningOnMatchingKeywords ---

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldSetWarningWhenKeywordMatchesAndDirectiveIsWarningOnMatchingKeywords()
    {
        var status = ExecutionStatus.CompletedAsSuccess;

        ExecutionEngine.ProcessLineForStatusUpdate("output contains ERROR keyword", ["ERROR"], JobModel.OutputProcessing.WarningOnMatchingKeywords, ref status);

        Assert.AreEqual(ExecutionStatus.CompletedAsWarning, status);
    }

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldNotChangeStatusWhenNoKeywordMatchAndDirectiveIsWarningOnMatchingKeywords()
    {
        var status = ExecutionStatus.CompletedAsSuccess;

        ExecutionEngine.ProcessLineForStatusUpdate("normal output", ["ERROR"], JobModel.OutputProcessing.WarningOnMatchingKeywords, ref status);

        Assert.AreEqual(ExecutionStatus.CompletedAsSuccess, status);
    }

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldNotDowngradeFromErrorWhenKeywordMatchesAndDirectiveIsWarningOnMatchingKeywords()
    {
        var status = ExecutionStatus.CompletedAsError;

        ExecutionEngine.ProcessLineForStatusUpdate("output contains ERROR keyword", ["ERROR"], JobModel.OutputProcessing.WarningOnMatchingKeywords, ref status);

        Assert.AreEqual(ExecutionStatus.CompletedAsError, status);
    }

    // --- ProcessLineForStatusUpdate: OutputProcessing.ErrorOnMatchingKeywords ---

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldSetErrorWhenKeywordMatchesAndDirectiveIsErrorOnMatchingKeywords()
    {
        var status = ExecutionStatus.CompletedAsSuccess;

        ExecutionEngine.ProcessLineForStatusUpdate("output contains ERROR keyword", ["ERROR"], JobModel.OutputProcessing.ErrorOnMatchingKeywords, ref status);

        Assert.AreEqual(ExecutionStatus.CompletedAsError, status);
    }

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldUpgradeFromWarningToErrorWhenKeywordMatchesAndDirectiveIsErrorOnMatchingKeywords()
    {
        var status = ExecutionStatus.CompletedAsWarning;

        ExecutionEngine.ProcessLineForStatusUpdate("output contains ERROR keyword", ["ERROR"], JobModel.OutputProcessing.ErrorOnMatchingKeywords, ref status);

        Assert.AreEqual(ExecutionStatus.CompletedAsError, status);
    }

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldNotChangeStatusWhenNoKeywordMatchAndDirectiveIsErrorOnMatchingKeywords()
    {
        var status = ExecutionStatus.CompletedAsSuccess;

        ExecutionEngine.ProcessLineForStatusUpdate("normal output", ["ERROR"], JobModel.OutputProcessing.ErrorOnMatchingKeywords, ref status);

        Assert.AreEqual(ExecutionStatus.CompletedAsSuccess, status);
    }

    // --- ProcessLineForStatusUpdate: keyword matching behavior ---

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldMatchKeywordsCaseSensitively()
    {
        var statusLower = ExecutionStatus.CompletedAsSuccess;
        var statusUpper = ExecutionStatus.CompletedAsSuccess;

        ExecutionEngine.ProcessLineForStatusUpdate("output contains error", ["ERROR"], JobModel.OutputProcessing.ErrorOnMatchingKeywords, ref statusLower);
        ExecutionEngine.ProcessLineForStatusUpdate("output contains ERROR", ["ERROR"], JobModel.OutputProcessing.ErrorOnMatchingKeywords, ref statusUpper);

        Assert.AreEqual(ExecutionStatus.CompletedAsSuccess, statusLower, "Lowercase 'error' should not match keyword 'ERROR'");
        Assert.AreEqual(ExecutionStatus.CompletedAsError, statusUpper, "Uppercase 'ERROR' should match keyword 'ERROR'");
    }

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldMatchWhenAnyKeywordInListMatches()
    {
        var status = ExecutionStatus.CompletedAsSuccess;

        ExecutionEngine.ProcessLineForStatusUpdate("output contains WARN", ["ERROR", "WARN", "FATAL"], JobModel.OutputProcessing.ErrorOnMatchingKeywords, ref status);

        Assert.AreEqual(ExecutionStatus.CompletedAsError, status);
    }

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldNotMatchWhenKeywordListIsEmpty()
    {
        var status = ExecutionStatus.CompletedAsSuccess;

        ExecutionEngine.ProcessLineForStatusUpdate("ERROR WARN FATAL", [], JobModel.OutputProcessing.ErrorOnMatchingKeywords, ref status);

        Assert.AreEqual(ExecutionStatus.CompletedAsSuccess, status);
    }
}
