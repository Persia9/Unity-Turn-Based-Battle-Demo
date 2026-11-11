#if UNITY_EDITOR
using System;
using Startup;
using Cysharp.Threading.Tasks;
using Infrastructure.Asset;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HotUpdate
{
    public sealed class EditorContentService : IAssetService, ISceneBundleLoader
    {
        public UniTask<T> Load<T>(string key) where T : UnityEngine.Object
        {
            var entry = LocalAssetCatalog.Get(key);
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(entry.Path);
            return asset != null ? UniTask.FromResult(asset)
                : UniTask.FromException<T>(new InvalidOperationException($"Missing editor asset: {key}"));
        }
        public UniTask Release(object assetOrInstance) => UniTask.CompletedTask;
        public async UniTask LoadScene(string sceneName)
        {
            if (sceneName != "Title" && sceneName != "Battle") throw new ArgumentException(sceneName);
            await UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                $"Assets/Scenes/{sceneName}.unity", new LoadSceneParameters(LoadSceneMode.Single));
        }
    }
}
#endif
