using Startup;
using Cysharp.Threading.Tasks;

namespace Infrastructure.Scene
{
    public sealed class SceneService : ISceneService
    {
        private readonly ISceneBundleLoader _loader;
        private readonly ILoadingScreen _screen;
        public SceneService(ISceneBundleLoader loader, ILoadingScreen screen)
        { _loader = loader; _screen = screen; }

        public async UniTask LoadScene(string sceneName)
        {
            _screen.Show(string.Empty);
            try { await _loader.LoadScene(sceneName); }
            catch { _screen.Hide(); throw; }
        }
    }
}
