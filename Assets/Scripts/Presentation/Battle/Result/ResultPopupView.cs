using System;
using Cysharp.Threading.Tasks;
using Domain.Run;
using Infrastructure.Localization;
using Infrastructure.UI;
using Presentation.Localization;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Zenject;

namespace Presentation.Battle.Result
{
    public sealed class ResultPopupView : UIView, IUIContextReceiver<ResultPopupContext>
    {
        [SerializeField] private TMP_Text _resultText;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private TMP_Text _statisticsText;
        [SerializeField] private TMP_Text _hpText;
        [SerializeField] private TMP_Text _attackText;
        [SerializeField] private TMP_Text _guardText;
        [SerializeField] private TMP_Text _guidanceText;
        [SerializeField] private TMP_Text _errorText;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private UpgradeSlot[] _upgrades;
        [Inject] private ResultPopupViewModel _viewModel;
        private ResultPopupContext _context;
        private IDisposable _binding;
        private TMP_Text[] _texts;
        private TMP_Text _retryLabel;
        private TMP_Text _closeLabel;
        private GameLanguage? _appliedLanguage;
        public void SetContext(ResultPopupContext context) => _context = context;

        public override void OnOpened()
        {
            _viewModel.Open(_context);
            _binding = _viewModel.State.Subscribe(Render);
            _closeButton.onClick.AddListener(OnTitleClicked);
            _retryButton.onClick.AddListener(OnRetryClicked);
            foreach (var slot in _upgrades)
            {
                slot.Button.onClick.AddListener(slot.Handler);
            }
        }

        public override void OnClosed()
        {
            _binding?.Dispose();
            _binding = null;
            _viewModel?.Close();
            _closeButton.onClick.RemoveListener(OnTitleClicked);
            _retryButton.onClick.RemoveListener(OnRetryClicked);
            foreach (var slot in _upgrades)
                if (slot.Handler != null)
                    slot.Button.onClick.RemoveListener(slot.Handler);
        }

        private void Awake()
        {
            _texts = GetComponentsInChildren<TMP_Text>(true);
            _retryLabel = _retryButton.GetComponentInChildren<TMP_Text>(true);
            _closeLabel = _closeButton.GetComponentInChildren<TMP_Text>(true);
            foreach (var slot in _upgrades)
            {
                slot.Label = slot.Button.GetComponentInChildren<TMP_Text>(true);
                var upgrade = slot.Upgrade;
                slot.Handler = () => _viewModel.Choose(upgrade).Forget();
            }
        }

        private void Render(ResultPopupViewModel.DisplayState state)
        {
            _titleText.text = state.Title;
            _resultText.text = state.Heading;
            _progressText.text = state.Progress;
            _statisticsText.text = state.Statistics;
            _hpText.text = state.Hp;
            _attackText.text = state.Attack;
            _guardText.text = state.Guard;
            _guidanceText.text = state.Guidance;
            _errorText.text = state.Error;
            _retryButton.gameObject.SetActive(!state.ShowUpgrades);
            _retryButton.interactable = _closeButton.interactable = state.CanInteract;
            _retryLabel.text = state.Retry;
            _closeLabel.text = state.Return;
            foreach (var slot in _upgrades)
            {
                slot.Button.gameObject.SetActive(state.ShowUpgrades);
                slot.Button.interactable = state.CanChoose(slot.Upgrade);
                slot.Label.text = state.UpgradeLabel(slot.Upgrade);
            }

            ApplyLanguage(state.Language);
        }

        private void ApplyLanguage(GameLanguage language)
        {
            if (_appliedLanguage == language) return;
            foreach (var text in _texts) LocalizedFontProvider.Apply(text, language);
            _appliedLanguage = language;
        }

        private void OnRetryClicked() => _viewModel.Retry().Forget();
        private void OnTitleClicked() => _viewModel.ReturnToTitle().Forget();
        private void OnDestroy() => OnClosed();

        [Serializable]
        private sealed class UpgradeSlot
        {
            public Button Button;
            public RunUpgrade Upgrade;
            [NonSerialized] public UnityAction Handler;
            [NonSerialized] public TMP_Text Label;
        }
    }
}