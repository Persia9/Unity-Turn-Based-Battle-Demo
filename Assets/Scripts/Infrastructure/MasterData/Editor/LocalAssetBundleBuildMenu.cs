using System.IO;
using System.Linq;
using Infrastructure.Asset;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Infrastructure.MasterData.Editor
{
    public sealed class LocalAssetBundleBuildMenu : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        [MenuItem("Tools/AssetBundles/Build Local Bundles")]
        public static void BuildLocalBundles() => Build(EditorUserBuildSettings.activeBuildTarget);

        public void OnPreprocessBuild(BuildReport report) => Build(report.summary.platform);

        private static void Build(BuildTarget target)
        {
            MasterDataBuildMenu.BuildFromExcel();
            var directory = Path.Combine(UnityEngine.Application.streamingAssetsPath, LocalAssetCatalog.DirectoryName);
            Directory.CreateDirectory(directory);
            foreach (var entry in LocalAssetCatalog.Entries)
                if (AssetDatabase.LoadMainAssetAtPath(entry.Path) == null)
                    throw new BuildFailedException($"Missing bundle asset: {entry.Path}");
            var manifest = BuildPipeline.BuildAssetBundles(directory, new[]
            {
                new AssetBundleBuild
                {
                    assetBundleName = LocalAssetCatalog.BundleName,
                    assetNames = LocalAssetCatalog.Entries.Select(x => x.Path).ToArray(),
                    addressableNames = LocalAssetCatalog.Entries.Select(x => x.Key.ToLowerInvariant()).ToArray()
                }
            }, BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode, target);
            if (manifest == null) throw new BuildFailedException("Local AssetBundle build failed.");
            AssetDatabase.Refresh();
            Debug.Log($"Local AssetBundle built for {target}: {directory}");
        }
    }
}
