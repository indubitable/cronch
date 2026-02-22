using cronch.Models;
using cronch.Models.Persistence;
using cronch.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace cronch.UnitTests.Services;

[TestClass]
public class SettingsServiceTests
{
    private ConfigPersistenceService _configPersistence = null!;
    private SettingsService _settingsService = null!;

    [TestInitialize]
    public void Setup()
    {
        _configPersistence = Substitute.For<ConfigPersistenceService>(
            Substitute.For<ILogger<ConfigPersistenceService>>(),
            Substitute.For<IConfiguration>());
        _settingsService = new SettingsService(_configPersistence);
    }

    // --- LoadSettings ---

    [TestMethod]
    public void LoadSettingsShouldReturnAllCompletionScriptFlagsAsFalseWhenNoConfigExists()
    {
        _configPersistence.Load().Returns((ConfigPersistenceModel?)null);

        var settings = _settingsService.LoadSettings();

        Assert.IsFalse(settings.RunCompletionScriptOnSuccess);
        Assert.IsFalse(settings.RunCompletionScriptOnWarning);
        Assert.IsFalse(settings.RunCompletionScriptOnError);
        Assert.IsFalse(settings.RunCompletionScriptOnIndeterminate);
    }

    [TestMethod]
    public void LoadSettingsShouldSetOnlySuccessFlagWhenOnlySuccessIsInRunCompletionScriptOn()
    {
        _configPersistence.Load().Returns(new ConfigPersistenceModel
        {
            RunCompletionScriptOn = ["Success"]
        });

        var settings = _settingsService.LoadSettings();

        Assert.IsTrue(settings.RunCompletionScriptOnSuccess);
        Assert.IsFalse(settings.RunCompletionScriptOnWarning);
        Assert.IsFalse(settings.RunCompletionScriptOnError);
        Assert.IsFalse(settings.RunCompletionScriptOnIndeterminate);
    }

    [TestMethod]
    public void LoadSettingsShouldSetAllFourFlagsWhenAllFourStatusesAreInRunCompletionScriptOn()
    {
        _configPersistence.Load().Returns(new ConfigPersistenceModel
        {
            RunCompletionScriptOn = ["Success", "Warning", "Error", "Indeterminate"]
        });

        var settings = _settingsService.LoadSettings();

        Assert.IsTrue(settings.RunCompletionScriptOnSuccess);
        Assert.IsTrue(settings.RunCompletionScriptOnWarning);
        Assert.IsTrue(settings.RunCompletionScriptOnError);
        Assert.IsTrue(settings.RunCompletionScriptOnIndeterminate);
    }

    // --- SaveSettings ---

    [TestMethod]
    public void SaveSettingsShouldPersistAllFourStatusStringsWhenAllFlagsAreTrue()
    {
        _configPersistence.Load().Returns(new ConfigPersistenceModel());
        ConfigPersistenceModel? saved = null;
        _configPersistence.When(c => c.Save(Arg.Any<ConfigPersistenceModel>()))
            .Do(call => saved = call.Arg<ConfigPersistenceModel>());

        _settingsService.SaveSettings(new SettingsModel
        {
            RunCompletionScriptOnSuccess = true,
            RunCompletionScriptOnWarning = true,
            RunCompletionScriptOnError = true,
            RunCompletionScriptOnIndeterminate = true,
        });

        Assert.IsNotNull(saved);
        Assert.HasCount(4, saved.RunCompletionScriptOn);
        CollectionAssert.Contains(saved.RunCompletionScriptOn, "Success");
        CollectionAssert.Contains(saved.RunCompletionScriptOn, "Warning");
        CollectionAssert.Contains(saved.RunCompletionScriptOn, "Error");
        CollectionAssert.Contains(saved.RunCompletionScriptOn, "Indeterminate");
    }

    [TestMethod]
    public void SaveSettingsShouldPersistEmptyListWhenNoFlagsAreSet()
    {
        _configPersistence.Load().Returns(new ConfigPersistenceModel());
        ConfigPersistenceModel? saved = null;
        _configPersistence.When(c => c.Save(Arg.Any<ConfigPersistenceModel>()))
            .Do(call => saved = call.Arg<ConfigPersistenceModel>());

        _settingsService.SaveSettings(new SettingsModel());

        Assert.IsNotNull(saved);
        Assert.IsEmpty(saved.RunCompletionScriptOn);
    }
}
