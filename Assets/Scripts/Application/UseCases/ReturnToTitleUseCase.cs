using Core.Common;
using Cysharp.Threading.Tasks;
using Infrastructure.Scene;

namespace Application.UseCases
{
    public sealed class ReturnToTitleUseCase
    {
        private readonly ISceneService _scenes;
        public ReturnToTitleUseCase(ISceneService scenes) => _scenes = scenes;
        public UniTask Execute() => _scenes.LoadScene(GameConstants.TitleSceneName);
    }
}
