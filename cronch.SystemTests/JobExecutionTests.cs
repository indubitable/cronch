namespace cronch.SystemTests;

/// <summary>
/// Tests for job execution: manual runs, output capture, and execution lifecycle.
/// </summary>
[TestClass]
[SystemTestCondition]
public class JobExecutionTests : SystemTestBase
{
    [TestMethod]
    public async Task ShouldRunJobManuallyAndComplete()
    {
        await CreateJobViaUiAsync("Manual Run Job", "Write-Output 'hello world'");

        // Trigger run from Manage page
        await RunJobFromManagePageAsync("Manual Run Job");

        // The toast message should indicate success
        var toast = Page.Locator(".toast-body:has-text('Job started')");
        await Assertions.Expect(toast).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ShouldCaptureJobOutputInExecutionDetails()
    {
        await CreateJobViaUiAsync("Output Job", "Write-Output 'CRONCH_TEST_OUTPUT_12345'");

        await RunJobFromManagePageAsync("Output Job");

        // Navigate to execution details via the toast link
        var detailsLink = Page.Locator(".toast-body a:text('View execution details')");
        var href = await detailsLink.GetAttributeAsync("href");
        Assert.IsNotNull(href);

        // Wait for execution to finish
        var status = await WaitForExecutionCompleteAsync($"{BaseUrl}{href}");

        Assert.AreEqual("Completed successfully", status);

        // Verify the output is displayed
        var output = Page.Locator(".job-output");
        await Assertions.Expect(output).ToContainTextAsync("CRONCH_TEST_OUTPUT_12345");
    }

    [TestMethod]
    public async Task ShouldShowExecutionAsSuccessForCleanExit()
    {
        await CreateJobViaUiAsync("Success Job", "Write-Output 'success'; exit 0");

        await RunJobFromManagePageAsync("Success Job");

        var detailsLink = Page.Locator(".toast-body a:text('View execution details')");
        var href = await detailsLink.GetAttributeAsync("href");
        Assert.IsNotNull(href);

        var status = await WaitForExecutionCompleteAsync($"{BaseUrl}{href}");

        Assert.AreEqual("Completed successfully", status);
    }

    [TestMethod]
    public async Task ShouldShowExecutionAsErrorForNonZeroExit()
    {
        await CreateJobViaUiAsync("Failing Job", "Write-Output 'failing'; exit 1");

        await RunJobFromManagePageAsync("Failing Job");

        var detailsLink = Page.Locator(".toast-body a:text('View execution details')");
        var href = await detailsLink.GetAttributeAsync("href");
        Assert.IsNotNull(href);

        var status = await WaitForExecutionCompleteAsync($"{BaseUrl}{href}");

        Assert.AreEqual("Completed with error", status);
    }

    [TestMethod]
    public async Task ShouldShowExecutionMetadata()
    {
        await CreateJobViaUiAsync("Metadata Job", "Write-Output 'metadata test'");

        await RunJobFromManagePageAsync("Metadata Job");

        var detailsLink = Page.Locator(".toast-body a:text('View execution details')");
        var href = await detailsLink.GetAttributeAsync("href");
        Assert.IsNotNull(href);

        await WaitForExecutionCompleteAsync($"{BaseUrl}{href}");

        // Verify execution metadata is displayed
        var jobNameCell = Page.Locator("#job-metadata td:right-of(th:text('Job name'))").First;
        await Assertions.Expect(jobNameCell).ToContainTextAsync("Metadata Job");

        var terminationCell = Page.Locator("#job-metadata td:right-of(th:text('Termination reason'))").First;
        await Assertions.Expect(terminationCell).ToContainTextAsync("Exited normally");
    }
}
