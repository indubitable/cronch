namespace cronch.SystemTests;

/// <summary>
/// Tests for job CRUD operations: create, edit, duplicate, delete.
/// </summary>
[TestClass]
[SystemTestCondition]
public class JobManagementTests : SystemTestBase
{
    [TestMethod]
    public async Task ShouldCreateJobAndShowOnManagePage()
    {
        await CreateJobViaUiAsync("Test Job Alpha", "Write-Output 'hello'");

        var jobCell = Page.Locator("td:text-is('Test Job Alpha')");
        await Assertions.Expect(jobCell).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ShouldShowNewJobAsDisabledByDefault()
    {
        await CreateJobViaUiAsync("Disabled Job", "Write-Output 'disabled'", enabled: false);

        var row = Page.Locator("tr", new() { Has = Page.Locator("td:text-is('Disabled Job')") });
        var toggle = row.Locator(".form-check-input[disabled]");
        await Assertions.Expect(toggle).Not.ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task ShouldEditJobName()
    {
        await CreateJobViaUiAsync("Original Name", "Write-Output 'original'");

        // Navigate to edit page
        await Page.GotoAsync($"{BaseUrl}/Manage");
        var row = Page.Locator("tr", new() { Has = Page.Locator("td:text-is('Original Name')") });
        await row.Locator("a:text('Configure')").ClickAsync();

        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);

        // Edit the name
        var nameInput = Page.Locator("input[name='Name']");
        await nameInput.ClearAsync();
        await nameInput.FillAsync("Updated Name");

        // Submit via direct POST
        await Page.EvaluateAsync(@"
            const form = document.querySelector('#job-form-container form');
            form.removeAttribute('hx-boost');
            form.submit();
        ");

        await Page.WaitForURLAsync($"{BaseUrl}/Manage");

        // Verify the updated name appears
        await Assertions.Expect(Page.Locator("td:text-is('Updated Name')")).ToBeVisibleAsync();
        await Assertions.Expect(Page.Locator("td:text-is('Original Name')")).Not.ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ShouldDeleteJob()
    {
        await CreateJobViaUiAsync("Job To Delete", "Write-Output 'delete me'");

        await Page.GotoAsync($"{BaseUrl}/Manage");
        await Assertions.Expect(Page.Locator("td:text-is('Job To Delete')")).ToBeVisibleAsync();

        // Open the overflow menu and click Delete
        var row = Page.Locator("tr", new() { Has = Page.Locator("td:text-is('Job To Delete')") });
        await row.Locator(".dropdown > button").ClickAsync();
        await row.Locator("button:text('Delete')").ClickAsync();

        // Confirm in the modal
        var modal = Page.Locator("#confirmDeleteModal");
        await Assertions.Expect(modal).ToBeVisibleAsync();
        await modal.Locator("button:text('Delete')").ClickAsync();

        // Verify job is gone
        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);
        await Assertions.Expect(Page.Locator("td:text-is('Job To Delete')")).Not.ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ShouldDuplicateJob()
    {
        await CreateJobViaUiAsync("Original Job", "Write-Output 'original'");

        await Page.GotoAsync($"{BaseUrl}/Manage");

        // Open the overflow menu and click Duplicate
        var row = Page.Locator("tr", new() { Has = Page.Locator("td:text-is('Original Job')") });
        await row.Locator(".dropdown > button").ClickAsync();
        await row.Locator("button:text('Duplicate')").ClickAsync();

        // Fill in duplicate name in the modal and confirm
        var modal = Page.Locator("#duplicateModal");
        await Assertions.Expect(modal).ToBeVisibleAsync();
        await modal.Locator("input[name='duplicateName']").FillAsync("Duplicated Job");
        await modal.Locator("button[type='submit']").ClickAsync();

        // Should redirect to edit page for the duplicate
        await Page.WaitForURLAsync(new System.Text.RegularExpressions.Regex($@"{System.Text.RegularExpressions.Regex.Escape(BaseUrl)}/EditJob"));

        // Verify the name on the edit page
        var nameInput = Page.Locator("input[name='Name']");
        await Assertions.Expect(nameInput).ToHaveValueAsync("Duplicated Job");
    }

    [TestMethod]
    public async Task ShouldShowCronScheduleOnManagePage()
    {
        await CreateJobViaUiAsync("Scheduled Job", "Write-Output 'scheduled'", cronSchedule: "0 0 * * * ?");

        await Page.GotoAsync($"{BaseUrl}/Manage");

        var row = Page.Locator("tr", new() { Has = Page.Locator("td:text-is('Scheduled Job')") });
        var scheduleCell = row.Locator("code:text('0 0 * * * ?')");
        await Assertions.Expect(scheduleCell).ToBeVisibleAsync();
    }
}
