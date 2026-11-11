using Startup;
using System;
using Application.UseCases;
using Cysharp.Threading.Tasks;
using Infrastructure.UI;
using Presentation.Title;
using Zenject;

namespace SceneFlow.Title
{
    public sealed class TitleSceneEntryPoint : IInitializable
    {
        private readonly ILoadingScreen _screen;
        private readonly StartupReady _ready;
        private readonly InitializeMasterDataUseCase _initializeMasterDataUseCase;
        private readonly IUIService _uiService;

        public TitleSceneEntryPoint(InitializeMasterDataUseCase initializeMasterDataUseCase, IUIService uiService, ILoadingScreen screen, StartupReady ready)
        {
            _initializeMasterDataUseCase = initializeMasterDataUseCase;
            _uiService = uiService;
            _screen = screen; _ready = ready;
        }

        public void Initialize()
        {
            Run().Forget();
        }

        private async UniTask Run()
        {
            try
            {
                await _initializeMasterDataUseCase.Execute();
                await _uiService.Open<TitleScreenView>("TitleScreen");
                await UniTask.NextFrame();
                _screen.Hide();
                _ready.Complete();
            }
            catch (Exception error)
            {
                _ready.Fail(error);
                _screen.Fail(error);
            }
        }
    }
}
