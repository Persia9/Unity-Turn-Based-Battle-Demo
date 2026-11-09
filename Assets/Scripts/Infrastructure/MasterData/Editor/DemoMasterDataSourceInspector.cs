#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Infrastructure.MasterData.Editor
{
    [CustomEditor(typeof(DemoMasterDataSource))]
    public sealed class DemoMasterDataSourceInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            var source = (DemoMasterDataSource)target;
            if (GUILayout.Button("Validate Data"))
            {
                var errors = MasterDataSourceValidator.Validate(source.Data);
                if (errors.Count == 0) Debug.Log("Master data validation passed.", source);
                else Debug.LogError(string.Join("\n", errors), source);
            }

            if (GUILayout.Button("Build MasterMemory Binaries")) MasterDataBuildMenu.Build(source);
        }
    }
}
#endif