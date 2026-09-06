using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Infrastructure.Scene
{
    public sealed class UnitySceneService : ISceneService
    {
        public async UniTask LoadScene(string sceneName)
        {
            await SceneManager.LoadSceneAsync(sceneName);
        }
    }
}