namespace cronch.SystemTests;

/// <summary>
/// Tests for job chaining: automatically triggering downstream jobs based on execution status.
/// </summary>
[TestClass]
[SystemTestCondition]
public class JobChainingTests : SystemTestBase
{
    [TestMethod]
    public async Task ShouldTriggerChainedJobOnSuccess()
    {
        // Create the target job first (Job B) — must be enabled for chaining
        await CreateJobViaUiAsync("Chain Target B", "Write-Output 'chained job ran'", enabled: true);
        var targetJobId = await GetJobIdFromManagePageAsync("Chain Target B");

        // Create the source job (Job A) and add a chain rule: on success → target B
        await CreateJobViaUiAsync("Chain Source A", "Write-Output 'source job'", enabled: true);
        await AddChainRuleToJobAsync("Chain Source A", targetJobId, onSuccess: true);

        // Run Job A 
        var executionUrl = await RunJobAndGetExecutionUrlAsync("Chain Source A");
        var status = await WaitForExecutionCompleteAsync(executionUrl);
        Assert.AreEqual("Completed successfully", status);

        // Verify the chained job also ran by checking history
        await Page.GotoAsync($"{BaseUrl}/History");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait briefly for the chained execution to appear
        var chainedRow = Page.Locator("tr:has(td:text-is('Chain Target B'))");
        await Assertions.Expect(chainedRow.First).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ShouldShowChainInfoInExecutionOutput()
    {
        // Create target and source jobs
        await CreateJobViaUiAsync("Chain Info Target", "Write-Output 'target ran'", enabled: true);
        var targetJobId = await GetJobIdFromManagePageAsync("Chain Info Target");

        await CreateJobViaUiAsync("Chain Info Source", "Write-Output 'source ran'", enabled: true);
        await AddChainRuleToJobAsync("Chain Info Source", targetJobId, onSuccess: true);

        // Run source job and wait for completion
        var executionUrl = await RunJobAndGetExecutionUrlAsync("Chain Info Source");
        await WaitForExecutionCompleteAsync(executionUrl);

        // The output should contain chain info mentioning the triggered job
        var output = Page.Locator(".job-output");
        await Assertions.Expect(output).ToContainTextAsync("Triggered chained job");
        await Assertions.Expect(output).ToContainTextAsync("Chain Info Target");
    }

    [TestMethod]
    public async Task ShouldNotTriggerChainedJobWhenConditionNotMet()
    {
        // Create target job
        await CreateJobViaUiAsync("No Chain Target", "Write-Output 'should not run'", enabled: true);
        var targetJobId = await GetJobIdFromManagePageAsync("No Chain Target");

        // Create source job chained on error only, but the job will succeed
        await CreateJobViaUiAsync("No Chain Source", "Write-Output 'succeeding'", enabled: true);
        await AddChainRuleToJobAsync("No Chain Source", targetJobId, onError: true);

        // Run source job — it succeeds, so error chain should NOT trigger
        var executionUrl = await RunJobAndGetExecutionUrlAsync("No Chain Source");
        await WaitForExecutionCompleteAsync(executionUrl);

        // Verify the chained job did NOT run — history should only show source execution
        await Page.GotoAsync($"{BaseUrl}/History");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Allow a moment for any chained runs to appear (they shouldn't)
        await Task.Delay(2000);
        await Page.ReloadAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var targetRows = Page.Locator("tr:has(td:text-is('No Chain Target'))");
        await Assertions.Expect(targetRows).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task ShouldTriggerChainedJobOnError()
    {
        // Create target that should fire on error
        await CreateJobViaUiAsync("Error Chain Target", "Write-Output 'error chain ran'", enabled: true);
        var targetJobId = await GetJobIdFromManagePageAsync("Error Chain Target");

        // Source job exits with error code
        await CreateJobViaUiAsync("Error Chain Source", "Write-Output 'failing'; exit 1", enabled: true);
        await AddChainRuleToJobAsync("Error Chain Source", targetJobId, onError: true);

        var executionUrl = await RunJobAndGetExecutionUrlAsync("Error Chain Source");
        var status = await WaitForExecutionCompleteAsync(executionUrl);
        Assert.AreEqual("Completed with error", status);

        // Wait for chained execution to appear in history
        await Page.GotoAsync($"{BaseUrl}/History");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var chainedRow = Page.Locator("tr:has(td:text-is('Error Chain Target'))");
        await Assertions.Expect(chainedRow.First).ToBeVisibleAsync();
    }
}
