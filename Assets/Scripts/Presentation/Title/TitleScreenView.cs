using System;
using Cysharp.Threading.Tasks;
using Domain.Run;
using Infrastructure.Localization;
using Infrastructure.UI;
using Presentation.Localization;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Presentation.Title
{
    public sealed class TitleScreenView : UIView
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _endlessButton;
        [SerializeField] private Button _languageButton;
        [SerializeField] private TMP_Text _genreLabel;
        [SerializeField] private TMP_Text _subtitleLabel;
        [SerializeField] private TMP_Text _taglineLabel;
        [SerializeField] private TMP_Text _startLabel;
        [SerializeField] private TMP_Text _endlessLabel;
        [SerializeField] private TMP_Text _languageLabel;
        [SerializeField] private TMP_Text _errorLabel;
        [Inject] private TitleScreenViewModel _viewModel;
        private IDisposable _binding;
        private TMP_Text[] _texts;
        private GameLanguage? _appliedLanguage;

        public override void OnOpened()
        {
            _viewModel.Open();
            _binding = _viewModel.State.Subscribe(Render);
            _startButton.onClick.AddListener(OnExpeditionClicked);
            _endlessButton.onClick.AddListener(OnEndlessClicked);
            _languageButton.onClick.AddListener(OnLanguageClicked);
        }

        public override void OnClosed()
        {
            _startButton.onClick.RemoveListener(OnExpeditionClicked);
            _endlessButton.onClick.RemoveListener(OnEndlessClicked);
            _languageButton.onClick.RemoveListener(OnLanguageClicked);
            _binding?.Dispose();
            _binding = null;
            _viewModel?.Close();
        }

        private void Awake()
        {
            _texts = GetComponentsInChildren<TMP_Text>(true);
        }

        private void OnExpeditionClicked() => _viewModel.Start(RunMode.Expedition).Forget();
        private void OnEndlessClicked() => _viewModel.Start(RunMode.Endless).Forget();
        private void OnLanguageClicked() => _viewModel.ToggleLanguage();

        private void Render(TitleScreenViewModel.DisplayState state)
        {
            _genreLabel.text = state.Genre;
            _subtitleLabel.text = state.Subtitle;
            _taglineLabel.text = state.Tagline;
            _startLabel.text = state.Expedition;
            _endlessLabel.text = state.Endless;
            _languageLabel.text = state.LanguageLabel;
            _errorLabel.text = state.Error;
            _startButton.interactable = _endlessButton.interactable = _languageButton.interactable = state.CanStart;
            ApplyLanguage(state.Language);
        }

        private void ApplyLanguage(GameLanguage language)
        {
            if (_appliedLanguage == language) return;

            foreach (var text in _texts)
                LocalizedFontProvider.Apply(text, language);

            _appliedLanguage = language;
        }

        private void OnDestroy() => OnClosed();
    }
}