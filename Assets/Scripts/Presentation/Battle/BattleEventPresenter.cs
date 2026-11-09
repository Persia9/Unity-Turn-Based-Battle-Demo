using System;
using Application.Messages;
using Application.Services;
using Application.UseCases;
using Infrastructure.UI;
using Presentation.Battle.Result;
using Cysharp.Threading.Tasks;
using Domain.Battle;
using Infrastructure.Localization;
using MessagePipe;
using UnityEngine;
using Zenject;

namespace Presentation.Battle
{
    public sealed class BattleEventPresenter : IInitializable, IDisposable
    {
        private readonly ISubscriber<DamageAppliedMessage> _damageApplied;
        private readonly ISubscriber<TurnChangedMessage> _turnChanged;
        private readonly ISubscriber<BattleFinishedMessage> _battleFinished;

        private IDisposable _damageSubscription;
        private IDisposable _turnSubscription;
        private IDisposable _finishSubscription;
        private IDisposable _actionSubscription;
        private readonly ISubscriber<BattleActionResolvedMessage> _actionResolved;
        private readonly IBattleSession _session;
        private readonly RunFlowUseCase _runFlow;
        private readonly ILocalizationService _localization;

        private readonly BattleViewModel _viewModel;
        private readonly IPopupService _popupService;

        public BattleEventPresenter(
            ISubscriber<DamageAppliedMessage> damageApplied,
            ISubscriber<TurnChangedMessage> turnChanged,
            ISubscriber<BattleFinishedMessage> battleFinished,
            BattleViewModel viewModel,
            IPopupService popupService,
            ISubscriber<BattleActionResolvedMessage> actionResolved,
            IBattleSession session, RunFlowUseCase runFlow, ILocalizationService localization)
        {
            _damageApplied = damageApplied;
            _turnChanged = turnChanged;
            _battleFinished = battleFinished;
            _viewModel = viewModel;
            _popupService = popupService;
            _actionResolved = actionResolved; _session = session; _runFlow = runFlow;
            _localization = localization;
        }

        public void Initialize()
        {
            Debug.Log("BattleEventPresenter Initialize");
            _damageSubscription = _damageApplied.Subscribe(OnDamageApplied);
            _turnSubscription = _turnChanged.Subscribe(OnTurnChanged);
            _finishSubscription = _battleFinished.Subscribe(OnBattleFinished);
            _actionSubscription = _actionResolved.Subscribe(OnActionResolved);
        }

        private void OnDamageApplied(DamageAppliedMessage message)
        {
            if (message.TargetSide == BattleSide.Player)
            {
                _viewModel.PlayerHp.Value = message.RemainingHp;
                _viewModel.AppendLog(_localization.Get("log.player_damage", message.Damage));
            }
            else
            {
                _viewModel.EnemyHp.Value = message.RemainingHp;
                _viewModel.AppendLog(_localization.Get("log.enemy_damage", message.Damage));
            }
        }

        private void OnActionResolved(BattleActionResolvedMessage message)
        {
            var result = message.Result;
            _viewModel.AppendLog(result.Action == BattleAction.Defend
                ? _localization.Get("log.player_defends")
                : _localization.Get("log.action", Side(result.Actor), _viewModel.ActionLabel(result.Action)));
            _viewModel.RefreshCommands(_session.State);
        }

        private void OnTurnChanged(TurnChangedMessage message)
        {
            _viewModel.TurnCount.Value = message.TurnCount;
            _viewModel.CanPlayerAct.Value = message.TurnOwner == TurnOwner.Player;
            _viewModel.TurnText.Value = _localization.Get("battle.turn", message.TurnCount, Side(message.TurnOwner));
            _viewModel.RefreshCommands(_session.State);
            _viewModel.AppendLog(_localization.Get("log.turn_changed", Side(message.TurnOwner)));
        }

        private void OnBattleFinished(BattleFinishedMessage message)
        {
            _viewModel.RefreshCommands(_session.State);
            _viewModel.CanPlayerAct.Value = false;
            _viewModel.AppendLog(_localization.Get("log.battle_finished", Side(message.Winner)));
            if (!_runFlow.CompleteBattle()) return;
            OpenResultPopup(message.Winner).Forget();
        }

        private async UniTask OpenResultPopup(BattleSide winner)
        {
            try
            {
                await _popupService.OpenPopup<ResultPopupView, ResultPopupContext>(
                    "ResultPopup", new ResultPopupContext(winner == BattleSide.Player));
            }
            catch (Exception error)
            {
                Debug.LogError("Failed to open ResultPopup after battle completion.");
                Debug.LogException(error);
                _viewModel.AppendLog(_localization.Get("log.result_failed"));
            }
        }

        private string Side(BattleSide side) => _localization.Get(side == BattleSide.Player ? "side.player" : "side.enemy");
        private string Side(TurnOwner owner) => _localization.Get(owner == TurnOwner.Player ? "side.player" : "side.enemy");

        public void Dispose()
        {
            _damageSubscription?.Dispose();
            _turnSubscription?.Dispose();
            _finishSubscription?.Dispose();
            _actionSubscription?.Dispose();
        }
    }
}
