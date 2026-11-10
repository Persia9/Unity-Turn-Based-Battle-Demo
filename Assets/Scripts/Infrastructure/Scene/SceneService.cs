using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Core.Common;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Infrastructure.Scene
{
    public sealed class SceneService : ISceneService
    {
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly ISceneLoadingOverlay _loadingOverlay;

        public SceneService(ISceneLoadingOverlay loadingOverlay) => _loadingOverlay = loadingOverlay;

        public async UniTask LoadScene(string sceneName)
        {
            await _gate.WaitAsync();
            try
            {
                _loadingOverlay.Show();
                var key = sceneName switch
                {
                    GameConstants.TitleSceneName => GameConstants.TitleSceneKey,
                    GameConstants.BattleSceneName => GameConstants.BattleSceneKey,
                    _ => sceneName
                };
                var handle = Addressables.LoadSceneAsync(key, LoadSceneMode.Single, true, 100,
                    SceneReleaseMode.ReleaseSceneWhenSceneUnloaded);
                while (!handle.IsDone) await UniTask.Yield();
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    var error = handle.OperationException ?? new InvalidOperationException($"Failed to load scene: {key}");
                    if (handle.IsValid()) Addressables.Release(handle);
                    throw error;
                }
            }
            catch
            {
                _loadingOverlay.Hide();
                throw;
            }
            finally { _gate.Release(); }
        }
    }
}
