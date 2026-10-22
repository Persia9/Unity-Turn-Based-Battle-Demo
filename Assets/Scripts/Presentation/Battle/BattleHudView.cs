using Application.Services;
using Application.UseCases;
using Cysharp.Threading.Tasks;
using Domain.Battle;
using Infrastructure.UI;
using R3;
using TMPro;
using UnityEngine;
using Presentation.Battle.Commands;
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
        [Inject] private PlayPlayerTurnUseCase _playTurn;
        [Inject] private IBattleSession _battleSession;
        [Inject] private RunSession _runSession;

        private BattleActionButton[] _actionButtons;
        private CompositeDisposable _disposables;
        private System.Threading.CancellationTokenSource _turnCancellation;

        public override void OnOpened()
        {
            _disposables = new CompositeDisposable();
            _turnCancellation = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            _actionButtons = GetComponentsInChildren<BattleActionButton>(true);
            foreach (var button in _actionButtons) button.Bind(_viewModel, ExecuteAction);

            _viewModel.TurnText.Subscribe(x => _turnText.text = x).AddTo(_disposables);
            _viewModel.PlayerHp.Subscribe(_ => RefreshPlayerHp()).AddTo(_disposables);
            _viewModel.PlayerMaxHp.Subscribe(_ => RefreshPlayerHp()).AddTo(_disposables);
            _viewModel.EnemyHp.Subscribe(_ => RefreshEnemyHp()).AddTo(_disposables);
            _viewModel.EnemyMaxHp.Subscribe(_ => RefreshEnemyHp()).AddTo(_disposables);
            _viewModel.LogText.Subscribe(x => _logText.text = x).AddTo(_disposables);
            _viewModel.EnemyIntentText.Subscribe(x => _enemyIntentText.text = x).AddTo(_disposables);
            _viewModel.DefenseText.Subscribe(x => _defenseText.text = x).AddTo(_disposables);

            var state = _battleSession.State;
            var run = _runSession.State;
            _runText.text = $"Battle {run.Round}/3 - {state.Enemy.Name} | Attack +{run.DamageBonus} | Guard {100 - run.GuardDamagePercent}%";
            _viewModel.PlayerMaxHp.Value = state.Player.MaxHp;
            _viewModel.PlayerHp.Value = state.Player.Hp;
            _viewModel.EnemyMaxHp.Value = state.Enemy.MaxHp;
            _viewModel.EnemyHp.Value = state.Enemy.Hp;
            _viewModel.TurnCount.Value = state.TurnCount;
            _viewModel.CanPlayerAct.Value = !state.IsFinished && state.CurrentTurn == TurnOwner.Player;
            _viewModel.TurnText.Value = $"Turn {state.TurnCount} / {state.CurrentTurn}";
            _viewModel.RefreshCommands(state);
            _viewModel.AppendLog("Battle Start: choose an action.");
        }

        public override void OnClosed()
        {
            if (_actionButtons != null)
                foreach (var button in _actionButtons) if (button != null) button.Unbind();
            _disposables?.Dispose();
            _turnCancellation?.Cancel();
            _turnCancellation?.Dispose();
            _turnCancellation = null;
        }

        private void RefreshPlayerHp()
        {
            _playerHpText.text = $"{_viewModel.PlayerHp.Value} / {_viewModel.PlayerMaxHp.Value}";
        }

        private void RefreshEnemyHp()
        {
            _enemyHpText.text = $"{_viewModel.EnemyHp.Value} / {_viewModel.EnemyMaxHp.Value}";
        }

        private void ExecuteAction(BattleAction action)
        {
            if (_turnCancellation != null)
                _playTurn.Execute(action, _turnCancellation.Token).Forget();
        }

        private void OnDestroy() => OnClosed();
    }
}
