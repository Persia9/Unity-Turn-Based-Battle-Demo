using Application.UseCases;
using Cysharp.Threading.Tasks;
using Infrastructure.UI;
using Infrastructure.Scene;
using Presentation.Title;
using Zenject;

namespace SceneFlow.Title
{
    public sealed class TitleSceneEntryPoint : IInitializable
    {
        private readonly InitializeMasterDataUseCase _initializeMasterDataUseCase;
        private readonly ISceneLoadingOverlay _loadingOverlay;
        private readonly IUIService _uiService;

        public TitleSceneEntryPoint(InitializeMasterDataUseCase initializeMasterDataUseCase, IUIService uiService, ISceneLoadingOverlay loadingOverlay)
        {
            _initializeMasterDataUseCase = initializeMasterDataUseCase;
            _uiService = uiService;
            _loadingOverlay = loadingOverlay;
        }

        public void Initialize()
        {
            _loadingOverlay.Show();
            Run().Forget();
        }

        private async UniTask Run()
        {
            try
            {
                await _initializeMasterDataUseCase.Execute();
                await _uiService.Open<TitleScreenView>("TitleScreen");
                // UI のレイアウト更新が完了するまでローディング表示を維持する。
                await UniTask.NextFrame();
                _loadingOverlay.Hide();
            }
            catch
            {
                _loadingOverlay.Hide();
                throw;
            }
        }
    }
}
