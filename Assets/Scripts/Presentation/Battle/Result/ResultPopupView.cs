using Infrastructure.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Application.UseCases;
using Cysharp.Threading.Tasks;
using Zenject;

namespace Presentation.Battle.Result
{
    public sealed class ResultPopupView : UIView, IUIContextReceiver<ResultPopupContext>
    {
        [SerializeField] private TMP_Text _resultText;
        [SerializeField] private Button _closeButton;

        private ResultPopupContext _context;
        [Inject] private ReturnToTitleUseCase _returnToTitle;

        public void SetContext(ResultPopupContext context)
        {
            _context = context;
        }

        public override void OnOpened()
        {
            _resultText.text = _context.IsWin ? "Victory" : "Defeat";
            _closeButton.onClick.AddListener(OnCloseClicked);
        }

        public override void OnClosed()
        {
            _closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        private void OnCloseClicked()
        {
            _closeButton.interactable = false;
            _returnToTitle.Execute().Forget();
        }
    }
}
