using UnityEditor;

namespace Infrastructure.MasterData.Editor
{
    public static class LocalAssetBundleBuildMenu
    {
        [MenuItem("Tools/AssetBundles/Build Local Bundles")]
        public static void BuildLocalBundles() => HybridCLRBuildMenu.PrepareClient();
    }
}
