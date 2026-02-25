namespace cronch.SystemTests;

/// <summary>
/// Tests for output processing rules: how stdout/stderr content affects execution status.
/// </summary>
[TestClass]
[SystemTestCondition]
public class OutputProcessingTests : SystemTestBase
{
    [TestMethod]
    public async Task ShouldPromoteToErrorOnAnyStderrOutput()
    {
        // Job exits with code 0 (success) but writes to stderr
        // StdErrProcessing = ErrorOnAnyOutput should promote status to error
        await CreateJobViaUiAsync("Stderr Error Job", "[Console]::Error.WriteLine('error output')",
            stdErrProcessing: "ErrorOnAnyOutput");

        var executionUrl = await RunJobAndGetExecutionUrlAsync("Stderr Error Job");
        var status = await WaitForExecutionCompleteAsync(executionUrl);

        Assert.AreEqual("Completed with error", status);
    }

    [TestMethod]
    public async Task ShouldPromoteToWarningOnAnyStderrOutput()
    {
        await CreateJobViaUiAsync("Stderr Warning Job", "[Console]::Error.WriteLine('warning output')",
            stdErrProcessing: "WarningOnAnyOutput");

        var executionUrl = await RunJobAndGetExecutionUrlAsync("Stderr Warning Job");
        var status = await WaitForExecutionCompleteAsync(executionUrl);

        Assert.AreEqual("Completed with warning", status);
    }

    [TestMethod]
    public async Task ShouldRemainSuccessWhenNoStderrOutput()
    {
        // Same processing rule, but no stderr output → should stay success
        await CreateJobViaUiAsync("Clean Stderr Job", "Write-Output 'clean stdout only'",
            stdErrProcessing: "ErrorOnAnyOutput");

        var executionUrl = await RunJobAndGetExecutionUrlAsync("Clean Stderr Job");
        var status = await WaitForExecutionCompleteAsync(executionUrl);

        Assert.AreEqual("Completed successfully", status);
    }

    [TestMethod]
    public async Task ShouldPromoteToErrorOnStdoutKeywordMatch()
    {
        // StdOutProcessing = ErrorOnMatchingKeywords with keyword "FAILURE"
        await CreateJobViaUiAsync("Keyword Error Job", "Write-Output 'detected FAILURE in build'",
            stdOutProcessing: "ErrorOnMatchingKeywords", keywords: "FAILURE");

        var executionUrl = await RunJobAndGetExecutionUrlAsync("Keyword Error Job");
        var status = await WaitForExecutionCompleteAsync(executionUrl);

        Assert.AreEqual("Completed with error", status);
    }

    [TestMethod]
    public async Task ShouldNotPromoteWhenKeywordNotPresent()
    {
        // Same keyword rule but output does NOT contain the keyword
        await CreateJobViaUiAsync("No Keyword Job", "Write-Output 'everything is fine'",
            stdOutProcessing: "ErrorOnMatchingKeywords", keywords: "FAILURE");

        var executionUrl = await RunJobAndGetExecutionUrlAsync("No Keyword Job");
        var status = await WaitForExecutionCompleteAsync(executionUrl);

        Assert.AreEqual("Completed successfully", status);
    }

    [TestMethod]
    public async Task ShouldPromoteToWarningOnStdoutKeywordMatch()
    {
        await CreateJobViaUiAsync("Keyword Warning Job", "Write-Output 'WARN something happened'",
            stdOutProcessing: "WarningOnMatchingKeywords", keywords: "WARN");

        var executionUrl = await RunJobAndGetExecutionUrlAsync("Keyword Warning Job");
        var status = await WaitForExecutionCompleteAsync(executionUrl);

        Assert.AreEqual("Completed with warning", status);
    }
}
