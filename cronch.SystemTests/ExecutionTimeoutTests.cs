namespace cronch.SystemTests;

/// <summary>
/// Tests for execution time limits: jobs that exceed their configured timeout are killed.
/// </summary>
[TestClass]
[SystemTestCondition]
public class ExecutionTimeoutTests : SystemTestBase
{
    [TestMethod]
    public async Task ShouldTerminateJobAfterTimeLimit()
    {
        // Create a long-running job with a short time limit so it gets killed
        await CreateJobViaUiAsync("Timeout Job", "Start-Sleep -Seconds 60", timeLimitSecs: 2);

        var executionUrl = await RunJobAndGetExecutionUrlAsync("Timeout Job");
        var status = await WaitForExecutionCompleteAsync(executionUrl, TimeSpan.FromSeconds(30));

        Assert.AreEqual("Completed with error", status);
    }

    [TestMethod]
    public async Task ShouldShowTimedOutTerminationReason()
    {
        await CreateJobViaUiAsync("Timeout Reason Job", "Start-Sleep -Seconds 60", timeLimitSecs: 2);

        var executionUrl = await RunJobAndGetExecutionUrlAsync("Timeout Reason Job");
        await WaitForExecutionCompleteAsync(executionUrl, TimeSpan.FromSeconds(30));

        var terminationCell = Page.Locator("#job-metadata td:right-of(th:text('Termination reason'))").First;
        await Assertions.Expect(terminationCell).ToContainTextAsync("Timed out");
    }

    [TestMethod]
    public async Task ShouldCompleteNormallyWithinTimeLimit()
    {
        // Job finishes well within its time limit
        await CreateJobViaUiAsync("Fast Enough Job", "Write-Output 'done quickly'", timeLimitSecs: 30);

        var executionUrl = await RunJobAndGetExecutionUrlAsync("Fast Enough Job");
        var status = await WaitForExecutionCompleteAsync(executionUrl);

        Assert.AreEqual("Completed successfully", status);

        var terminationCell = Page.Locator("#job-metadata td:right-of(th:text('Termination reason'))").First;
        await Assertions.Expect(terminationCell).ToContainTextAsync("Exited normally");
    }
}
