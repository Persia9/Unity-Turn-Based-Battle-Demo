using Application.UseCases;
using Cysharp.Threading.Tasks;
using Infrastructure.UI;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Presentation.Title
{
    public sealed class TitleScreenView : UIView
    {
        [SerializeField] private Button _startButton;
        [Inject] private StartBattleUseCase _startBattleUseCase;

        public override void OnOpened()
        {
            _startButton.onClick.AddListener(OnStartClicked);
        }

        public override void OnClosed()
        {
            _startButton.onClick.RemoveListener(OnStartClicked);
        }

        private void OnStartClicked()
        {
            Run().Forget();
        }

        private async UniTask Run()
        {
            await _startBattleUseCase.Execute(1);
        }
    }
}