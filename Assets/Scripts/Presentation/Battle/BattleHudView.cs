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
        [Inject] private AttackUseCase _attackUseCase;
        [Inject] private EnemyTurnRunner _enemyTurnRunner;
        [Inject] private IBattleSession _battleSession;

        private readonly CompositeDisposable _disposables = new();

        public override void OnOpened()
        {
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
            _viewModel.TurnText.Value = $"Turn {state.TurnCount} / Player";
            _viewModel.AppendLog("Battle Start");
        }

        public override void OnClosed()
        {
            _attackButton.onClick.RemoveListener(OnAttackClicked);
            _disposables.Dispose();
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
            _attackUseCase.ExecutePlayerAttack();
            if (_battleSession.State.CurrentTurn == TurnOwner.Enemy && !_battleSession.State.IsFinished)
            {
                RunEnemyTurn().Forget();
            }
        }

        private async UniTask RunEnemyTurn()
        {
            await _enemyTurnRunner.RunAsync();
        }
    }
}