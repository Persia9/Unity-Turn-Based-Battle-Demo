using System;
using Application.Messages;
using Application.Services;
using Domain.Battle;
using MessagePipe;
using UnityEngine;
using Zenject;

namespace Presentation.Battle.Characters
{
    public sealed class CubeBattleCharacterPresenter : IInitializable, IDisposable
    {
        private readonly BattleCharacterAnchors _anchors;
        private readonly IBattleSession _session;
        private readonly ISubscriber<BattleActionResolvedMessage> _actions;
        private IDisposable _subscription;
        private CubeBattleCharacterView _player;
        private CubeBattleCharacterView _enemy;
        private GameObject _platform;

        public CubeBattleCharacterPresenter(BattleCharacterAnchors anchors, IBattleSession session,
            ISubscriber<BattleActionResolvedMessage> actions)
        {
            _anchors = anchors;
            _session = session;
            _actions = actions;
        }

        public void Initialize()
        {
            var state = _session.State;
            var playerName = string.IsNullOrWhiteSpace(state.Player.Name) ? "Player" : state.Player.Name;
            var enemyName = string.IsNullOrWhiteSpace(state.Enemy.Name) ? "Enemy" : state.Enemy.Name;
            _player = UnityEngine.Object.Instantiate(_anchors.PlayerPrefab, _anchors.PlayerRoot, false);
            _player.name = playerName;
            _enemy = UnityEngine.Object.Instantiate(_anchors.EnemyPrefab, _anchors.EnemyRoot, false);
            _enemy.name = enemyName;
            _platform = UnityEngine.Object.Instantiate(_anchors.PlatformPrefab, _anchors.PlayerRoot.parent, false);
            _subscription = _actions.Subscribe(OnActionResolved);
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            if (_player != null) UnityEngine.Object.Destroy(_player.gameObject);
            if (_enemy != null) UnityEngine.Object.Destroy(_enemy.gameObject);
            if (_platform != null) UnityEngine.Object.Destroy(_platform);
        }

        private void OnActionResolved(BattleActionResolvedMessage message)
        {
            var result = message.Result;
            var actor = result.Actor == BattleSide.Player ? _player : _enemy;
            var target = result.Target == BattleSide.Player ? _player : _enemy;
            var direction = target.transform.position - actor.transform.position;

            if (result.Action == BattleAction.Defend)
            {
                actor.PlayDefend();
                return;
            }

            actor.PlayAttack(result.Action == BattleAction.StrongAttack, direction);
            target.PlayHit(result.RemainingHp <= 0, direction);
        }
    }
}