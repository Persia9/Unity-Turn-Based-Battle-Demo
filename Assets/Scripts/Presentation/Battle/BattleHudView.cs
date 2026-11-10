using Cysharp.Threading.Tasks;
using Domain.Battle;
using Infrastructure.Localization;
using Infrastructure.UI;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Presentation.Battle.Commands;
using Presentation.Localization;
using Zenject;

namespace Presentation.Battle
{
    public sealed class BattleHudView : UIView
    {
        [SerializeField] private TMP_Text _enemyIntentText;
        [SerializeField] private TMP_Text _defenseText;
        [SerializeField] private TMP_Text _turnText;
        [SerializeField] private TMP_Text _runText;
        [SerializeField] private TMP_Text _playerHpText;
        [SerializeField] private TMP_Text _enemyHpText;
        [SerializeField] private TMP_Text _logText;

        [Inject] private BattleViewModel _viewModel;

        private BattleActionButton[] _actionButtons;
        private TMP_Text[] _texts;
        private GameLanguage? _appliedLanguage;
        private CompositeDisposable _disposables;
        [SerializeField] private ScrollRect _logScroll;

        public override void OnOpened()
        {
            _disposables = new CompositeDisposable();
            _viewModel.Open(destroyCancellationToken);
            foreach (var button in _actionButtons) button.Bind(_viewModel, ExecuteAction);
            _viewModel.Language.Subscribe(ApplyLanguage).AddTo(_disposables);

            _viewModel.TurnText.Subscribe(x => _turnText.text = x).AddTo(_disposables);
            _viewModel.PlayerHpText.Subscribe(x => _playerHpText.text = x).AddTo(_disposables);
            _viewModel.EnemyHpText.Subscribe(x => _enemyHpText.text = x).AddTo(_disposables);
            _viewModel.RunText.Subscribe(x => _runText.text = x).AddTo(_disposables);
            _viewModel.LogText.Subscribe(RefreshLog).AddTo(_disposables);
            _viewModel.EnemyIntentText.Subscribe(x => _enemyIntentText.text = x).AddTo(_disposables);
            _viewModel.DefenseText.Subscribe(x => _defenseText.text = x).AddTo(_disposables);
        }

        public override void OnClosed()
        {
            if (_actionButtons != null)
                foreach (var button in _actionButtons)
                    if (button != null)
                        button.Unbind();
            _disposables?.Dispose();
            _viewModel?.Close();
        }

        private void Awake()
        {
            _actionButtons = GetComponentsInChildren<BattleActionButton>(true);
            _texts = GetComponentsInChildren<TMP_Text>(true);
        }

        private void ApplyLanguage(GameLanguage language)
        {
            if (_appliedLanguage == language) return;
            foreach (var text in _texts) LocalizedFontProvider.Apply(text, language);
            _appliedLanguage = language;
        }

        private void RefreshLog(string value)
        {
            _logText.text = value;
            ScrollLogToLatest().Forget();
        }

        private async UniTaskVoid ScrollLogToLatest()
        {
            if (await UniTask.NextFrame(cancellationToken: destroyCancellationToken)
                    .SuppressCancellationThrow()) return;
            if (_logScroll == null || _logScroll.content == null) return;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_logScroll.content);
            _logScroll.StopMovement();
            _logScroll.verticalNormalizedPosition = 0f;
        }

        private void ExecuteAction(BattleAction action) => _viewModel.ExecuteAction(action).Forget();

        private void OnDestroy() => OnClosed();
    }
}