using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Core.Common;
using Infrastructure.MasterData.Editor;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Infrastructure.ContentUpdate.Editor
{
    public static class RemoteContentBuildMenu
    {
        private const string TitlePrefab = "Assets/Prefabs/UI/Title/TitleScreenView.prefab";
        private const string GroupName = "Remote Content";
        private static string Root => Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
        private static string ReleaseRoot => Path.Combine(Root, "Artifacts", "ContentUpdate",
            EditorUserBuildSettings.activeBuildTarget.ToString(), Uri.EscapeDataString(PlayerSettings.bundleVersion));
        private static string Baseline => Path.Combine(ReleaseRoot, "Baseline");
        private static string BaselineState => Path.Combine(Baseline, "addressables_content_state.bin");

        [MenuItem("Tools/Content Update/1 Configure Remote Content")]
        public static void Configure()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) throw new InvalidOperationException("Addressables settings are missing.");
            settings.BuildRemoteCatalog = true;
            settings.DisableCatalogUpdateOnStartup = true;
            settings.CatalogRequestsTimeout = 15;
            settings.UniqueBundleIds = true;
            settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
            settings.RemoteCatalogBuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            settings.RemoteCatalogLoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
            var url = settings.profileSettings.GetValueByName(settings.activeProfileId, AddressableAssetSettings.kRemoteLoadPath);
            if (string.IsNullOrWhiteSpace(url) || url == "<undefined>")
                settings.profileSettings.SetValue(settings.activeProfileId, AddressableAssetSettings.kRemoteLoadPath,
                    "http://127.0.0.1:8080/[BuildTarget]");
            var group = settings.FindGroup(GroupName);
            if (group == null) group = settings.CreateGroup(GroupName, false, false, false, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.AppendHash;
            schema.Timeout = 30; schema.RetryCount = 2;
            group.GetSchema<ContentUpdateGroupSchema>().StaticContent = false;
            settings.AddLabel(GameConstants.RemoteContentLabel);
            Add(settings, group, "Assets/Scenes/Title.unity", GameConstants.TitleSceneKey);
            Add(settings, group, "Assets/Scenes/Battle.unity", GameConstants.BattleSceneKey);
            Add(settings, group, "Assets/MasterData/Binary/master_core.bytes", GameConstants.MasterCoreKey);
            Add(settings, group, "Assets/MasterData/Binary/master_ui.bytes", GameConstants.MasterUiKey);
            Add(settings, group, "Assets/MasterData/Binary/master_localization.bytes", GameConstants.MasterLocalizationKey);
            Add(settings, group, TitlePrefab, "UI/Title/TitleScreen");
            Add(settings, group, "Assets/Prefabs/UI/Battle/BattleHudView.prefab", "UI/Battle/BattleHud");
            Add(settings, group, "Assets/Prefabs/UI/Battle/ResultPopupView.prefab", "UI/Battle/ResultPopup");
            Add(settings, group, "Assets/Prefabs/BattleCharacters/PlayerCharacter.prefab", "Characters/Player");
            Add(settings, group, "Assets/Prefabs/BattleCharacters/EnemyCharacter.prefab", "Characters/Enemy");
            Add(settings, group, "Assets/Prefabs/BattleCharacters/BattlePlatform.prefab", "Characters/Platform");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(GameConstants.BootstrapScenePath, true) };
            // Local HTTP is for the development demo player. Production hosting should use HTTPS.
            PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;
            EditorUtility.SetDirty(settings); EditorUtility.SetDirty(group); EditorUtility.SetDirty(schema);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Content Update/2 Build Baseline Resources")]
        public static void BuildBaseline()
        {
            if (File.Exists(BaselineState)) throw new InvalidOperationException(
                "A baseline already exists for this app version. Use Build Content Update, or change the app version for a new release.");
            Configure(); MasterDataBuildMenu.BuildFromExcel();
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            Validate(result);
            Directory.CreateDirectory(Baseline);
            var state = ContentUpdateScript.GetContentStateDataPath(false, AddressableAssetSettingsDefaultObject.Settings);
            if (!File.Exists(state)) throw new FileNotFoundException("Addressables content state was not generated.", state);
            Snapshot(Path.Combine(Baseline, "ServerData"), null);
            CopyDirectory(Path.GetFullPath(Addressables.BuildPath), Path.Combine(Baseline, "ClientData"));
            File.WriteAllText(Path.Combine(Baseline, "release.txt"),
                $"Unity: {UnityEngine.Application.unityVersion}\nApp: {PlayerSettings.bundleVersion}\nTarget: {EditorUserBuildSettings.activeBuildTarget}\n");
            File.Copy(state, BaselineState, false);
            Debug.Log($"Baseline resources and original content state saved: {Baseline}");
        }

        [MenuItem("Tools/Content Update/3 Build Demo Player From Baseline")]
        public static void BuildDemoPlayer()
        {
            if (!File.Exists(BaselineState)) throw new InvalidOperationException("Build baseline resources first.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                throw new InvalidOperationException("This demo player command targets Windows 64-bit. Switch the active target first.");
            Configure();
            // Keep this client's starting catalog tied to v1 even if an update was built later.
            CopyDirectory(Path.Combine(Baseline, "ClientData"), Path.GetFullPath(Addressables.BuildPath));
            var directory = Path.Combine(ReleaseRoot, "Player"); Directory.CreateDirectory(directory);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { GameConstants.BootstrapScenePath }, target = BuildTarget.StandaloneWindows64,
                locationPathName = Path.Combine(directory, "CubeBattle.exe"), options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Demo player build failed.");
            Debug.Log($"Demo player: {directory}");
        }

        [MenuItem("Tools/Content Update/4 Build Content Update")]
        public static void BuildUpdate()
        {
            if (!File.Exists(BaselineState)) throw new FileNotFoundException("Build baseline resources first.", BaselineState);
            Configure(); MasterDataBuildMenu.BuildFromExcel();
            var result = ContentUpdateScript.BuildContentUpdate(AddressableAssetSettingsDefaultObject.Settings, BaselineState);
            Validate(result);
            var folder = Path.Combine(ReleaseRoot, "Updates", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
            Snapshot(Path.Combine(folder, "ServerData"), Path.Combine(Baseline, "ServerData"));
            Debug.Log($"Update files (overlay on the original server directory): {folder}");
        }

        [MenuItem("Tools/Content Update/5 Build Sample Title v2 Update")]
        public static void BuildSampleUpdate()
        {
            if (!File.Exists(BaselineState)) throw new FileNotFoundException("Build baseline resources and the player first.", BaselineState);
            var file = Path.Combine(Root, TitlePrefab); var original = File.ReadAllBytes(file);
            try
            {
                var contents = PrefabUtility.LoadPrefabContents(TitlePrefab);
                try
                {
                    var title = contents.GetComponentsInChildren<TMP_Text>(true).Single(x => x.gameObject.name == "Game Title");
                    title.text = "CUBE BATTLE v2";
                    PrefabUtility.SaveAsPrefabAsset(contents, TitlePrefab);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
                BuildUpdate();
            }
            finally
            {
                File.WriteAllBytes(file, original);
                AssetDatabase.ImportAsset(TitlePrefab, ImportAssetOptions.ForceUpdate);
            }
        }

        private static void Add(AddressableAssetSettings settings, AddressableAssetGroup group, string path, string address)
        {
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid)) throw new FileNotFoundException("Required content asset is missing.", path);
            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = address; entry.SetLabel(GameConstants.RemoteContentLabel, true, true);
        }
        private static void Validate(AddressablesPlayerBuildResult result)
        {
            if (result == null || !string.IsNullOrEmpty(result.Error))
                throw new InvalidOperationException(result?.Error ?? "Addressables build returned no result.");
        }
        private static void Snapshot(string destination, string baseline)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var source = Path.GetFullPath(settings.profileSettings.EvaluateString(settings.activeProfileId,
                settings.profileSettings.GetValueByName(settings.activeProfileId, AddressableAssetSettings.kRemoteBuildPath)));
            if (!Directory.Exists(source)) throw new DirectoryNotFoundException(source);
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(source, file);
                var publishedPath = Path.Combine(EditorUserBuildSettings.activeBuildTarget.ToString(), relative);
                if (baseline != null && Equal(file, Path.Combine(baseline, publishedPath))) continue;
                var target = Path.Combine(destination, publishedPath);
                Directory.CreateDirectory(Path.GetDirectoryName(target)); File.Copy(file, target, true);
            }
        }
        private static bool Equal(string first, string second)
        {
            if (!File.Exists(second) || new FileInfo(first).Length != new FileInfo(second).Length) return false;
            using var sha = SHA256.Create(); using var a = File.OpenRead(first); using var b = File.OpenRead(second);
            return sha.ComputeHash(a).SequenceEqual(sha.ComputeHash(b));
        }
        private static void CopyDirectory(string source, string destination)
        {
            if (!Directory.Exists(source)) throw new DirectoryNotFoundException(source);
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var target = Path.Combine(destination, Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(file, target, true);
            }
        }
    }
}
