using Application.UseCases;
using Cysharp.Threading.Tasks;
using Infrastructure.UI;
using Presentation.Title;
using Zenject;

namespace SceneFlow.Title
{
    public sealed class TitleSceneEntryPoint : IInitializable
    {
        private readonly InitializeMasterDataUseCase _initializeMasterDataUseCase;
        private readonly IUIService _uiService;

        public TitleSceneEntryPoint(InitializeMasterDataUseCase initializeMasterDataUseCase, IUIService uiService)
        {
            _initializeMasterDataUseCase = initializeMasterDataUseCase;
            _uiService = uiService;
        }

        public void Initialize()
        {
            Run().Forget();
        }

        private async UniTask Run()
        {
            await _initializeMasterDataUseCase.Execute();
            await _uiService.Open<TitleScreenView>("TitleScreen");
        }
    }
}
