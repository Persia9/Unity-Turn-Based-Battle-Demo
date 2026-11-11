using System;
using Cysharp.Threading.Tasks;

namespace Startup
{
    public interface ISceneBundleLoader { UniTask LoadScene(string sceneName); }
    public interface ILoadingScreen
    {
        void Show(string message);
        void Hide();
        void Fail(Exception exception);
    }
    public sealed class StartupReady
    {
        private readonly UniTaskCompletionSource _ready = new();
        public UniTask Task => _ready.Task;
        public void Complete() => _ready.TrySetResult();
        public void Fail(Exception exception) => _ready.TrySetException(exception);
    }
}
