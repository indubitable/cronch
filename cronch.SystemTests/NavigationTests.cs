namespace cronch.SystemTests;

/// <summary>
/// Tests for site-wide navigation, layout elements, and static pages.
/// </summary>
[TestClass]
[SystemTestCondition]
public class NavigationTests : SystemTestBase
{
    [TestMethod]
    public async Task ShouldNavigateToAllMainPages()
    {
        var pages = new (string LinkText, string ExpectedTitle)[]
        {
            ("Overview", "Overview - CRONCH!"),
            ("Manage", "Manage - CRONCH!"),
            ("History", "History - CRONCH!"),
            ("Settings", "Settings - CRONCH!"),
        };

        foreach (var (linkText, expectedTitle) in pages)
        {
            await Page.GotoAsync(BaseUrl);
            await Page.Locator($"nav a.nav-link:text-is('{linkText}')").ClickAsync();
            await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);

            var title = await Page.TitleAsync();
            Assert.AreEqual(expectedTitle, title, $"Navigation to {linkText} failed");
        }
    }

    [TestMethod]
    public async Task ShouldShowBrandLinkToHome()
    {
        await Page.GotoAsync($"{BaseUrl}/Settings");

        await Page.Locator("a.navbar-brand:text('CRONCH!')").ClickAsync();
        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);

        var title = await Page.TitleAsync();
        Assert.AreEqual("Overview - CRONCH!", title);
    }

    [TestMethod]
    public async Task ShouldShowAboutPage()
    {
        await Page.GotoAsync(BaseUrl);

        await Page.Locator("footer a:text('About')").ClickAsync();
        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);

        var title = await Page.TitleAsync();
        Assert.AreEqual("About - CRONCH!", title);
    }

    [TestMethod]
    public async Task ShouldShowAddJobPageFromManage()
    {
        await Page.GotoAsync($"{BaseUrl}/Manage");

        await Page.Locator("a:text('Add Job')").ClickAsync();
        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);

        var title = await Page.TitleAsync();
        Assert.AreEqual("Add Job - CRONCH!", title);
    }

    [TestMethod]
    public async Task ShouldReturn404ForNonexistentExecutionDetails()
    {
        var fakeId = Guid.NewGuid();
        var response = await Page.GotoAsync($"{BaseUrl}/ExecutionDetails/{fakeId}");

        Assert.IsNotNull(response);
        Assert.AreEqual(404, response.Status);
    }

    [TestMethod]
    public async Task ShouldReturn404ForNonexistentEditJob()
    {
        var fakeId = Guid.NewGuid();
        var response = await Page.GotoAsync($"{BaseUrl}/EditJob/{fakeId}");

        Assert.IsNotNull(response);
        Assert.AreEqual(404, response.Status);
    }
}
