using System;
using Application.Messages;
using Infrastructure.UI;
using Presentation.Battle.Result;
using Cysharp.Threading.Tasks;
using Domain.Battle;
using MessagePipe;
using UnityEngine;
using Zenject;

namespace Presentation.Battle
{
    public sealed class BattleEventPresenter : IInitializable, IDisposable
    {
        private readonly ISubscriber<BattleStartedMessage> _battleStarted;
        private readonly ISubscriber<DamageAppliedMessage> _damageApplied;
        private readonly ISubscriber<TurnChangedMessage> _turnChanged;
        private readonly ISubscriber<BattleFinishedMessage> _battleFinished;

        private IDisposable _d1;
        private IDisposable _d2;
        private IDisposable _d3;
        private IDisposable _d4;

        private readonly BattleViewModel _viewModel;
        private readonly IPopupService _popupService;

        public BattleEventPresenter(
            ISubscriber<BattleStartedMessage> battleStarted,
            ISubscriber<DamageAppliedMessage> damageApplied,
            ISubscriber<TurnChangedMessage> turnChanged,
            ISubscriber<BattleFinishedMessage> battleFinished,
            BattleViewModel viewModel,
            IPopupService popupService)
        {
            _battleStarted = battleStarted;
            _damageApplied = damageApplied;
            _turnChanged = turnChanged;
            _battleFinished = battleFinished;
            _viewModel = viewModel;
            _popupService = popupService;
        }

        public void Initialize()
        {
            Debug.Log("BattleEventPresenter Initialize");
            _d1 = _battleStarted.Subscribe(OnBattleStarted);
            _d2 = _damageApplied.Subscribe(OnDamageApplied);
            _d3 = _turnChanged.Subscribe(OnTurnChanged);
            _d4 = _battleFinished.Subscribe(OnBattleFinished);
        }

        private void OnBattleStarted(BattleStartedMessage message)
        {
            _viewModel.AppendLog("Battle started.");
        }

        private void OnDamageApplied(DamageAppliedMessage message)
        {
            if (message.TargetSide == BattleSide.Player)
            {
                _viewModel.PlayerHp.Value = message.RemainingHp;
                _viewModel.AppendLog($"Player takes {message.Damage} damage.");
            }
            else
            {
                _viewModel.EnemyHp.Value = message.RemainingHp;
                _viewModel.AppendLog($"Enemy takes {message.Damage} damage.");
            }
        }

        private void OnTurnChanged(TurnChangedMessage message)
        {
            _viewModel.TurnCount.Value = message.TurnCount;
            _viewModel.CanPlayerAct.Value = message.TurnOwner == TurnOwner.Player;
            _viewModel.TurnText.Value = $"Turn {message.TurnCount} / {message.TurnOwner}";
            _viewModel.AppendLog($"Turn changed: {message.TurnOwner}");
        }

        private void OnBattleFinished(BattleFinishedMessage message)
        {
            _viewModel.CanPlayerAct.Value = false;
            _viewModel.AppendLog($"Battle finished. Winner: {message.Winner}");
            _popupService.OpenPopup<ResultPopupView, ResultPopupContext>(
                "ResultPopup", new ResultPopupContext(message.Winner == BattleSide.Player)).Forget();
        }

        public void Dispose()
        {
            _d1?.Dispose();
            _d2?.Dispose();
            _d3?.Dispose();
            _d4?.Dispose();
        }
    }
}