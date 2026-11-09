using System;
using UnityEngine;
using Infrastructure.MasterData.Repositories;

namespace Infrastructure.Localization
{
    public sealed class LocalizationService : ILocalizationService
    {
        private const string PreferenceKey = "game.language";
        private readonly LocalizedTextLookup _text;
        public GameLanguage CurrentLanguage { get; private set; }
        public event Action LanguageChanged;

        public LocalizationService(ILocalizationRepository repository)
        {
            _text = new LocalizedTextLookup(repository);
            var saved = PlayerPrefs.GetString(PreferenceKey, string.Empty);
            CurrentLanguage = saved == "ja" || (string.IsNullOrEmpty(saved) &&
                                                UnityEngine.Application.systemLanguage == SystemLanguage.Japanese)
                ? GameLanguage.Japanese
                : GameLanguage.English;
        }

        public string Get(string key, params object[] arguments)
            => _text.Get(CurrentLanguage, key, arguments);

        public void ToggleLanguage()
        {
            CurrentLanguage = CurrentLanguage == GameLanguage.English ? GameLanguage.Japanese : GameLanguage.English;
            PlayerPrefs.SetString(PreferenceKey, CurrentLanguage == GameLanguage.Japanese ? "ja" : "en");
            PlayerPrefs.Save();
            LanguageChanged?.Invoke();
        }
    }
}