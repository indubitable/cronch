namespace cronch.SystemTests;

/// <summary>
/// Tests for the History page and Overview page execution display.
/// </summary>
[TestClass]
[SystemTestCondition]
public class HistoryTests : SystemTestBase
{
    [TestMethod]
    public async Task ShouldShowExecutionInHistory()
    {
        await CreateJobViaUiAsync("History Job", "Write-Output 'history test'");
        var executionUrl = await RunJobAndGetExecutionUrlAsync("History Job");
        await WaitForExecutionCompleteAsync(executionUrl);

        await Page.GotoAsync($"{BaseUrl}/History");

        var jobRow = Page.Locator("tr:has-text('History Job')");
        await Assertions.Expect(jobRow).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ShouldShowExecutionOnOverviewPage()
    {
        await CreateJobViaUiAsync("Overview Job", "Write-Output 'overview test'");
        var executionUrl = await RunJobAndGetExecutionUrlAsync("Overview Job");
        await WaitForExecutionCompleteAsync(executionUrl);

        await Page.GotoAsync(BaseUrl);

        // The overview page shows recent executions
        var jobRow = Page.Locator("tr:has-text('Overview Job')");
        await Assertions.Expect(jobRow).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ShouldShowStatsOnOverviewPage()
    {
        await CreateJobViaUiAsync("Stats Job", "Write-Output 'stats test'");
        var executionUrl = await RunJobAndGetExecutionUrlAsync("Stats Job");
        await WaitForExecutionCompleteAsync(executionUrl);

        await Page.GotoAsync(BaseUrl);

        // Verify the stats section shows at least 1 success
        var successStat = Page.Locator(".text-success.fw-bold");
        var successText = await successStat.TextContentAsync();
        Assert.IsNotNull(successText);
        var successCount = int.Parse(successText.Trim());
        Assert.IsTrue(successCount >= 1, $"Expected at least 1 success, got {successCount}");
    }

    [TestMethod]
    public async Task ShouldNavigateFromHistoryToExecutionDetails()
    {
        await CreateJobViaUiAsync("Clickable Job", "Write-Output 'click test'");
        var executionUrl = await RunJobAndGetExecutionUrlAsync("Clickable Job");
        await WaitForExecutionCompleteAsync(executionUrl);

        await Page.GotoAsync($"{BaseUrl}/History");

        // Click on the execution row
        var jobRow = Page.Locator("tr:has-text('Clickable Job')");
        await jobRow.ClickAsync();

        // Should navigate to execution details
        await Page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(@"/ExecutionDetails/"));

        var title = await Page.TitleAsync();
        Assert.AreEqual("Job Execution Details - CRONCH!", title);
    }
}
