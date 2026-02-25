namespace cronch.SystemTests;

/// <summary>
/// Tests for manually terminating a running job execution via the UI.
/// </summary>
[TestClass]
[SystemTestCondition]
public class ExecutionTerminateTests : SystemTestBase
{
    [TestMethod]
    public async Task ShouldTerminateRunningJob()
    {
        // Create a long-running job
        await CreateJobViaUiAsync("Long Running Job", "Start-Sleep -Seconds 120");

        var executionUrl = await RunJobAndGetExecutionUrlAsync("Long Running Job");

        // Navigate to execution details and wait for it to be running
        await Page.GotoAsync(executionUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // The Terminate button should be visible while running
        var terminateButton = Page.Locator("button:text('Terminate')").First;

        // Wait for the execution to show as Running (may need a moment to start)
        await Assertions.Expect(terminateButton).ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Click the terminate button to open the confirmation modal
        await terminateButton.ClickAsync();

        // Confirm termination in the modal
        var modal = Page.Locator("#confirmKillModal");
        await Assertions.Expect(modal).ToBeVisibleAsync();
        await modal.Locator("button:text('Terminate')").ClickAsync();

        // Wait for the page to reload and show completed status
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for execution to complete (the redirect back after POST)
        var status = await WaitForExecutionCompleteAsync(Page.Url, TimeSpan.FromSeconds(15));
        Assert.AreNotEqual("Running", status);
    }

    [TestMethod]
    public async Task ShouldShowUserTriggeredTerminationReason()
    {
        await CreateJobViaUiAsync("Termination Reason Job", "Start-Sleep -Seconds 120");

        var executionUrl = await RunJobAndGetExecutionUrlAsync("Termination Reason Job");
        await Page.GotoAsync(executionUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var terminateButton = Page.Locator("button:text('Terminate')").First;
        await Assertions.Expect(terminateButton).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await terminateButton.ClickAsync();

        var modal = Page.Locator("#confirmKillModal");
        await Assertions.Expect(modal).ToBeVisibleAsync();
        await modal.Locator("button:text('Terminate')").ClickAsync();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await WaitForExecutionCompleteAsync(Page.Url, TimeSpan.FromSeconds(15));

        var terminationCell = Page.Locator("#job-metadata td:right-of(th:text('Termination reason'))").First;
        await Assertions.Expect(terminationCell).ToContainTextAsync("Terminated by user");
    }

    [TestMethod]
    public async Task ShouldNotShowTerminateButtonAfterCompletion()
    {
        await CreateJobViaUiAsync("Quick Complete Job", "Write-Output 'done'");

        var executionUrl = await RunJobAndGetExecutionUrlAsync("Quick Complete Job");
        await WaitForExecutionCompleteAsync(executionUrl);

        // The button should either not exist or not be visible
        await Assertions.Expect(Page.Locator("button:text('Terminate')")).Not.ToBeVisibleAsync();
    }
}
