namespace cronch.SystemTests;

/// <summary>
/// Tests for the Settings page: saving and persisting configuration.
/// </summary>
[TestClass]
[SystemTestCondition]
public class SettingsTests : SystemTestBase
{
    [TestMethod]
    public async Task ShouldLoadSettingsPage()
    {
        await Page.GotoAsync($"{BaseUrl}/Settings");

        var title = await Page.TitleAsync();
        Assert.AreEqual("Settings - CRONCH!", title);

        // Verify key form fields are present
        await Assertions.Expect(Page.Locator("input[name='SettingsVM.MaxHistoryItemsShown']")).ToBeVisibleAsync();
        await Assertions.Expect(Page.Locator("input[name='SettingsVM.MaxChainDepth']")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ShouldSaveAndPersistSettings()
    {
        await Page.GotoAsync($"{BaseUrl}/Settings");

        // Set a custom history limit
        var historyInput = Page.Locator("input[name='SettingsVM.MaxHistoryItemsShown']");
        await historyInput.ClearAsync();
        await historyInput.FillAsync("42");

        // Set a custom chain depth
        var chainInput = Page.Locator("input[name='SettingsVM.MaxChainDepth']");
        await chainInput.ClearAsync();
        await chainInput.FillAsync("5");

        // Submit via direct POST to avoid htmx
        await Page.EvaluateAsync(@"
            const form = document.querySelector('#settings-form-container form');
            form.removeAttribute('hx-boost');
            form.submit();
        ");

        await Page.WaitForURLAsync($"{BaseUrl}/Settings");

        // Toast should confirm save
        var toast = Page.Locator(".toast-body:has-text('Settings have been saved')");
        await Assertions.Expect(toast).ToBeVisibleAsync();

        // Verify the values persisted
        await Assertions.Expect(Page.Locator("input[name='SettingsVM.MaxHistoryItemsShown']")).ToHaveValueAsync("42");
        await Assertions.Expect(Page.Locator("input[name='SettingsVM.MaxChainDepth']")).ToHaveValueAsync("5");
    }

    [TestMethod]
    public async Task ShouldRejectInvalidHistoryLimit()
    {
        await Page.GotoAsync($"{BaseUrl}/Settings");

        // Set an out-of-range value (min is 10)
        var historyInput = Page.Locator("input[name='SettingsVM.MaxHistoryItemsShown']");
        await historyInput.ClearAsync();
        await historyInput.FillAsync("3");

        await Page.EvaluateAsync(@"
            const form = document.querySelector('#settings-form-container form');
            form.removeAttribute('hx-boost');
            form.submit();
        ");

        // Should stay on settings page with a validation error
        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);

        var validationError = Page.Locator(".text-danger");
        await Assertions.Expect(validationError.First).ToBeVisibleAsync();
    }
}
