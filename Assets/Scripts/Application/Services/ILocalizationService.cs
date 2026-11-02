using System;

namespace Application.Services
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
