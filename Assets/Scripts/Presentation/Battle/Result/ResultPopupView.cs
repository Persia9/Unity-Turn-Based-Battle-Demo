using Infrastructure.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Presentation.Battle.Result
{
    public sealed class ResultPopupView : UIView, IUIContextReceiver<ResultPopupContext>
    {
        [SerializeField] private TMP_Text _resultText;
        [SerializeField] private Button _closeButton;

        private ResultPopupContext _context;

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
            
        }
    }
}