namespace cronch.SystemTests;

/// <summary>
/// Tests for job parallelism limits: restricting concurrent executions of the same job.
/// </summary>
[TestClass]
[SystemTestCondition]
public class ParallelismTests : SystemTestBase
{
    [TestMethod]
    public async Task ShouldSkipExecutionWhenParallelismLimitReached()
    {
        // Create a long-running job with parallelism=1, marking skips as error
        await CreateJobViaUiAsync("Parallel Limited Job", "Start-Sleep -Seconds 30",
            parallelism: 1, markParallelSkipAs: "MarkAsError");

        // Run the job once — it starts and stays running
        await RunJobFromManagePageAsync("Parallel Limited Job");

        // Brief delay so the first execution registers
        await Task.Delay(1000);

        // Run it again — should be immediately skipped due to parallelism limit
        var secondExecUrl = await RunJobAndGetExecutionUrlAsync("Parallel Limited Job");

        // The second execution should immediately complete as skipped
        await Page.GotoAsync(secondExecUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var statusCell = Page.Locator("#job-metadata td:right-of(th:text('Status'))").First;
        var statusText = (await statusCell.TextContentAsync())?.Trim();
        Assert.AreEqual("Completed with error", statusText);

        var terminationCell = Page.Locator("#job-metadata td:right-of(th:text('Termination reason'))").First;
        await Assertions.Expect(terminationCell).ToContainTextAsync("Skipped due to parallelism limit");
    }

    [TestMethod]
    public async Task ShouldMarkParallelSkipAsWarning()
    {
        await CreateJobViaUiAsync("Parallel Warning Job", "Start-Sleep -Seconds 30",
            parallelism: 1, markParallelSkipAs: "MarkAsWarning");

        // First run starts and stays running
        await RunJobFromManagePageAsync("Parallel Warning Job");
        await Task.Delay(1000);

        // Second run should be skipped — marked as warning
        var secondExecUrl = await RunJobAndGetExecutionUrlAsync("Parallel Warning Job");

        await Page.GotoAsync(secondExecUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var statusCell = Page.Locator("#job-metadata td:right-of(th:text('Status'))").First;
        var statusText = (await statusCell.TextContentAsync())?.Trim();
        Assert.AreEqual("Completed with warning", statusText);
    }

    [TestMethod]
    public async Task ShouldAllowExecutionWhenNoParallelismLimit()
    {
        // Job with no parallelism limit should allow multiple concurrent runs
        await CreateJobViaUiAsync("Unlimited Parallel Job", "Start-Sleep -Seconds 15");

        // Run twice
        await RunJobFromManagePageAsync("Unlimited Parallel Job");
        await Task.Delay(500);
        await RunJobFromManagePageAsync("Unlimited Parallel Job");

        // Both should be running — check history for two executions
        await Page.GotoAsync($"{BaseUrl}/History");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var rows = Page.Locator("tr:has(td:text-is('Unlimited Parallel Job'))");
        var count = await rows.CountAsync();
        Assert.IsTrue(count >= 2, $"Expected at least 2 executions, found {count}");
    }
}
