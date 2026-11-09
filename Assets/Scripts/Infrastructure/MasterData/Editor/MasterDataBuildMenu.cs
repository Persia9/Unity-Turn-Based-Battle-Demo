#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using MasterMemory;
using UnityEditor;
using UnityEngine;

namespace Infrastructure.MasterData.Editor
{
    public static class MasterDataBuildMenu
    {
        public const string DefaultSourcePath = "Assets/MasterData/Source/DemoMasterData.asset";
        public const string CoreOutputPath = "Assets/MasterData/Binary/master_core.bytes";
        public const string UIOutputPath = "Assets/MasterData/Binary/master_ui.bytes";

        [MenuItem("Tools/MasterData/Select Data Source")]
        public static void SelectSource()
        {
            var source = LoadDefault();
            Selection.activeObject = source;
            EditorGUIUtility.PingObject(source);
        }

        [MenuItem("Tools/MasterData/Build Demo MasterData")]
        public static void BuildAll() => Build(LoadDefault());

        [MenuItem("Tools/MasterData/Build Selected Data Source")]
        public static void BuildSelected() => Build(Selection.activeObject as DemoMasterDataSource);

        [MenuItem("Tools/MasterData/Build Selected Data Source", true)]
        private static bool CanBuildSelected() => Selection.activeObject is DemoMasterDataSource;

        private static DemoMasterDataSource LoadDefault()
        {
            var source = AssetDatabase.LoadAssetAtPath<DemoMasterDataSource>(DefaultSourcePath);
            if (source == null)
                throw new InvalidOperationException($"Master data source is missing: {DefaultSourcePath}");
            return source;
        }

        public static void Build(DemoMasterDataSource source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var errors = MasterDataSourceValidator.Validate(source.Data);
            if (errors.Count != 0)
                throw new InvalidOperationException("Master data validation failed:\n" + string.Join("\n", errors));
            var data = source.Data;
            var core = new DatabaseBuilder();
            core.Append(data.Characters.Select(x => x.ToMaster()));
            core.Append(data.Skills.Select(x => x.ToMaster()));
            core.Append(data.Stages.Select(x => x.ToMaster()));
            var ui = new DatabaseBuilder();
            ui.Append(data.UI.Select(x => x.ToMaster()));
            var coreBytes = core.Build();
            var uiBytes = ui.Build();
            AssetDatabase.SaveAssets();
            Write(CoreOutputPath, coreBytes);
            Write(UIOutputPath, uiBytes);
            AssetDatabase.Refresh();
            Debug.Log(
                $"Master data built from {AssetDatabase.GetAssetPath(source)}: {coreBytes.Length} core bytes, {uiBytes.Length} UI bytes.");
        }

        private static void Write(string path, ReadOnlySpan<byte> bytes)
        {
            var absolute = Path.Combine(UnityEngine.Application.dataPath, "..", path);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllBytes(absolute, bytes.ToArray());
        }
    }
}
#endif