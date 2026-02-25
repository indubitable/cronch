namespace cronch.SystemTests;

[TestClass]
[SystemTestCondition]
public class SmokeTests : SystemTestBase
{
    [TestMethod]
    public async Task ShouldLoadHomePage()
    {
        await Page.GotoAsync(BaseUrl);

        var title = await Page.TitleAsync();

        Assert.AreEqual("Overview - CRONCH!", title);
    }

    [TestMethod]
    public async Task ShouldShowHealthyStatus()
    {
        using var client = new HttpClient();
        var response = await client.GetAsync($"{BaseUrl}/health");

        Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.AreEqual("Healthy", body);
    }

    [TestMethod]
    public async Task ShouldShowEmptyHistoryMessage()
    {
        await Page.GotoAsync($"{BaseUrl}/History");

        var emptyMessage = Page.Locator("text=No executions yet");
        await Assertions.Expect(emptyMessage).ToBeVisibleAsync();
    }
}
