using Cysharp.Threading.Tasks;

namespace Infrastructure.Scene
{
    public interface ISceneService
    {
        UniTask LoadScene(string sceneName);
    }
}