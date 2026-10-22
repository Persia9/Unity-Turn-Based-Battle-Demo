using System;
using Application.Messages;
using Application.Services;
using Application.UseCases;
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
        private IDisposable _d5;
        private readonly ISubscriber<BattleActionResolvedMessage> _actionResolved;
        private readonly IBattleSession _session;
        private readonly RunFlowUseCase _runFlow;

        private readonly BattleViewModel _viewModel;
        private readonly IPopupService _popupService;

        public BattleEventPresenter(
            ISubscriber<BattleStartedMessage> battleStarted,
            ISubscriber<DamageAppliedMessage> damageApplied,
            ISubscriber<TurnChangedMessage> turnChanged,
            ISubscriber<BattleFinishedMessage> battleFinished,
            BattleViewModel viewModel,
            IPopupService popupService,
            ISubscriber<BattleActionResolvedMessage> actionResolved,
            IBattleSession session, RunFlowUseCase runFlow)
        {
            _battleStarted = battleStarted;
            _damageApplied = damageApplied;
            _turnChanged = turnChanged;
            _battleFinished = battleFinished;
            _viewModel = viewModel;
            _popupService = popupService;
            _actionResolved = actionResolved; _session = session; _runFlow = runFlow;
        }

        public void Initialize()
        {
            Debug.Log("BattleEventPresenter Initialize");
            _d1 = _battleStarted.Subscribe(OnBattleStarted);
            _d2 = _damageApplied.Subscribe(OnDamageApplied);
            _d3 = _turnChanged.Subscribe(OnTurnChanged);
            _d4 = _battleFinished.Subscribe(OnBattleFinished);
            _d5 = _actionResolved.Subscribe(OnActionResolved);
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

        private void OnActionResolved(BattleActionResolvedMessage message)
        {
            var result = message.Result;
            _viewModel.AppendLog(result.Action == BattleAction.Defend
                ? "Player defends: next enemy damage is halved."
                : $"{result.Actor} uses {result.Action}.");
            _viewModel.RefreshCommands(_session.State);
        }

        private void OnTurnChanged(TurnChangedMessage message)
        {
            _viewModel.TurnCount.Value = message.TurnCount;
            _viewModel.CanPlayerAct.Value = message.TurnOwner == TurnOwner.Player;
            _viewModel.TurnText.Value = $"Turn {message.TurnCount} / {message.TurnOwner}";
            _viewModel.RefreshCommands(_session.State);
            _viewModel.AppendLog($"Turn changed: {message.TurnOwner}");
        }

        private void OnBattleFinished(BattleFinishedMessage message)
        {
            _viewModel.RefreshCommands(_session.State);
            _viewModel.CanPlayerAct.Value = false;
            _viewModel.AppendLog($"Battle finished. Winner: {message.Winner}");
            if (!_runFlow.CompleteBattle()) return;
            _popupService.OpenPopup<ResultPopupView, ResultPopupContext>(
                "ResultPopup", new ResultPopupContext(message.Winner == BattleSide.Player)).Forget();
        }

        public void Dispose()
        {
            _d1?.Dispose();
            _d2?.Dispose();
            _d3?.Dispose();
            _d4?.Dispose();
            _d5?.Dispose();
        }
    }
}