using System;

namespace Infrastructure.Asset
{
    public static class LocalAssetCatalog
    {
        public const string DirectoryName = "AssetBundles";
        public const string BundleName = "localcontent";

        public static readonly Entry[] Entries =
        {
            new("Master/Core", "Assets/MasterData/Binary/master_core.bytes"),
            new("Master/UI", "Assets/MasterData/Binary/master_ui.bytes"),
            new("Master/Localization", "Assets/MasterData/Binary/master_localization.bytes"),
            new("UI/Title/TitleScreen", "Assets/Prefabs/UI/Title/TitleScreenView.prefab"),
            new("UI/Battle/BattleHud", "Assets/Prefabs/UI/Battle/BattleHudView.prefab"),
            new("UI/Battle/ResultPopup", "Assets/Prefabs/UI/Battle/ResultPopupView.prefab")
        };

        public static Entry Get(string key)
        {
            foreach (var entry in Entries)
                if (entry.Key == key) return entry;
            throw new ArgumentException($"Unknown asset key: {key}", nameof(key));
        }

        public readonly struct Entry
        {
            public string Key { get; }
            public string Path { get; }
            public Entry(string key, string path) { Key = key; Path = path; }
        }
    }
}
