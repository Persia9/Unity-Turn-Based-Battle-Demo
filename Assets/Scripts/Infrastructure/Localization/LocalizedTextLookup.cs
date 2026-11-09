using System;
using System.Globalization;
using Application.Services;
using Infrastructure.MasterData.Repositories;

namespace Infrastructure.Localization
{
    public sealed class LocalizedTextLookup
    {
        private readonly ILocalizationRepository _repository;
        public LocalizedTextLookup(ILocalizationRepository repository) => _repository = repository;
        public string Get(GameLanguage language, string key, params object[] arguments)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (!_repository.TryGet(key, out var row)) return key;
            var value = language == GameLanguage.Japanese && !string.IsNullOrEmpty(row.Japanese)
                ? row.Japanese : row.English;
            if (string.IsNullOrEmpty(value)) return key;
            return arguments == null || arguments.Length == 0
                ? value : string.Format(CultureInfo.InvariantCulture, value, arguments);
        }
    }
}
