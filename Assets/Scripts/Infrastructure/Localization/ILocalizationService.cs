using System;

namespace Infrastructure.Localization
{
    public enum GameLanguage
    {
        English,
        Japanese
    }

    public interface ILocalizationService
    {
        GameLanguage CurrentLanguage { get; }
        event Action LanguageChanged;
        string Get(string key, params object[] arguments);
        void ToggleLanguage();
    }
}