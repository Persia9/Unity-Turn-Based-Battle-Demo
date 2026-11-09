using System.Collections.Generic;
using Infrastructure.Localization;
using TMPro;
using UnityEngine;

namespace Presentation.Localization
{
    public static class LocalizedFontProvider
    {
        private const string JapaneseFontPath = "Localization/NotoSansJP";
        private static TMP_FontAsset _japaneseFont;
        private static readonly Dictionary<TMP_Text, TMP_FontAsset> OriginalFonts = new();

        public static void Apply(TMP_Text text, ILocalizationService localization)
        {
            if (text == null) return;
            if (!OriginalFonts.ContainsKey(text)) OriginalFonts[text] = text.font;
            if (localization.CurrentLanguage != GameLanguage.Japanese)
            {
                text.font = OriginalFonts[text];
                return;
            }
            if (_japaneseFont == null)
            {
                var source = Resources.Load<Font>(JapaneseFontPath);
                if (source == null)
                {
                    Debug.LogError($"Japanese font was not found at Resources/{JapaneseFontPath}.");
                    return;
                }
                _japaneseFont = TMP_FontAsset.CreateFontAsset(source);
                _japaneseFont.name = "Noto Sans JP (Runtime)";
                _japaneseFont.isMultiAtlasTexturesEnabled = true;
            }
            text.font = _japaneseFont;
        }
    }
}
