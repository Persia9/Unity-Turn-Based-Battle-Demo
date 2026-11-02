using Application.Services;
using Infrastructure.Localization;
using NUnit.Framework;
using UnityEngine;

public sealed class LocalizationServiceTests
{
    private const string PreferenceKey = "game.language";

    [SetUp]
    public void SetUp() => PlayerPrefs.SetString(PreferenceKey, "en");

    [TearDown]
    public void TearDown() => PlayerPrefs.DeleteKey(PreferenceKey);

    [Test]
    public void SavedEnglishIsUsedAndFormatsArguments()
    {
        var localization = new UnityLocalizationService();
        Assert.That(localization.CurrentLanguage, Is.EqualTo(GameLanguage.English));
        Assert.That(localization.Get("battle.turn", 2, "PLAYER"), Is.EqualTo("TURN 2  •  PLAYER"));
    }

    [Test]
    public void ToggleSwitchesToJapaneseAndPersistsSelection()
    {
        var localization = new UnityLocalizationService();
        localization.ToggleLanguage();
        Assert.That(localization.CurrentLanguage, Is.EqualTo(GameLanguage.Japanese));
        Assert.That(localization.Get("action.attack"), Is.EqualTo("攻撃"));
        Assert.That(localization.Get("result.title"), Is.EqualTo("遠征  //  戦闘結果"));
        Assert.That(new UnityLocalizationService().CurrentLanguage, Is.EqualTo(GameLanguage.Japanese));
    }

    [Test]
    public void MissingKeyIsVisibleInsteadOfReturningEmptyText()
    {
        Assert.That(new UnityLocalizationService().Get("missing.key"), Is.EqualTo("missing.key"));
    }
}
