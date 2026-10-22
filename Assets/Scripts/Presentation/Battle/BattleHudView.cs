using Application.Services;
using Application.UseCases;
using Cysharp.Threading.Tasks;
using Domain.Battle;
using Infrastructure.UI;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Presentation.Battle
{
    public sealed class BattleHudView : UIView
    {
        [SerializeField] private Button _attackButton;
        [SerializeField] private TMP_Text _turnText;
        [SerializeField] private TMP_Text _playerHpText;
        [SerializeField] private TMP_Text _enemyHpText;
        [SerializeField] private TMP_Text _logText;

        [Inject] private BattleViewModel _viewModel;
        [Inject] private PlayPlayerTurnUseCase _playTurn;
        [Inject] private IBattleSession _battleSession;

        private CompositeDisposable _disposables;
        private System.Threading.CancellationTokenSource _turnCancellation;

        public override void OnOpened()
        {
            _disposables = new CompositeDisposable();
            _turnCancellation = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            _attackButton.onClick.AddListener(OnAttackClicked);

            _viewModel.TurnText.Subscribe(x => _turnText.text = x).AddTo(_disposables);
            _viewModel.PlayerHp.Subscribe(_ => RefreshPlayerHp()).AddTo(_disposables);
            _viewModel.PlayerMaxHp.Subscribe(_ => RefreshPlayerHp()).AddTo(_disposables);
            _viewModel.EnemyHp.Subscribe(_ => RefreshEnemyHp()).AddTo(_disposables);
            _viewModel.EnemyMaxHp.Subscribe(_ => RefreshEnemyHp()).AddTo(_disposables);
            _viewModel.LogText.Subscribe(x => _logText.text = x).AddTo(_disposables);
            _viewModel.CanPlayerAct.Subscribe(x => _attackButton.interactable = x).AddTo(_disposables);

            var state = _battleSession.State;
            _viewModel.PlayerMaxHp.Value = state.Player.MaxHp;
            _viewModel.PlayerHp.Value = state.Player.Hp;
            _viewModel.EnemyMaxHp.Value = state.Enemy.MaxHp;
            _viewModel.EnemyHp.Value = state.Enemy.Hp;
            _viewModel.TurnCount.Value = state.TurnCount;
            _viewModel.CanPlayerAct.Value = !state.IsFinished && state.CurrentTurn == TurnOwner.Player;
            _viewModel.TurnText.Value = $"Turn {state.TurnCount} / {state.CurrentTurn}";
            _viewModel.AppendLog("Battle Start");
        }

        public override void OnClosed()
        {
            _attackButton.onClick.RemoveListener(OnAttackClicked);
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

        private void OnAttackClicked()
        {
            _playTurn.Execute(_turnCancellation.Token).Forget();
        }

        private void OnDestroy() => OnClosed();
    }
}
