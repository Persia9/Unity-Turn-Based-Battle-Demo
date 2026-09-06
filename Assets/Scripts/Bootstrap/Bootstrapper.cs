using Application.UseCases;
using Infrastructure.UI;
using Presentation.Title;
using UnityEngine;
using Zenject;

namespace Bootstrap
{
    public sealed class Bootstrapper : MonoBehaviour
    {
        [Inject] private InitializeMasterDataUseCase _initializeMasterDataUseCase;
        [Inject] private IUIService _uiService;

        private async void Start()
        {
            await _initializeMasterDataUseCase.Execute();
            await _uiService.Open<TitleScreenView>("TitleScreen");
        }
    }
}